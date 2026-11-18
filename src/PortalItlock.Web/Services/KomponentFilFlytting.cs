using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Flytter filene på varene (bilde, FDV, montasjeblad, datablad) ut av
// databasen og over i KomponentFilLager, litt om gangen:
//  1. Fila skrives under et midlertidig navn og kontrolleres.
//  2. Kolonnen tømmes - bare hvis den fortsatt har samme fil.
//  3. Først da blir fila på disken den gjeldende.
// Går noe galt, ligger fila fortsatt i databasen, og neste runde prøver igjen.
// Kjører ved oppstart og så hver time. Om natta krympes databasen (VACUUM) når
// filene er flyttet ut og det er nok ledig plass.
public class KomponentFilFlytting(IServiceScopeFactory scopeFactory, ILogger<KomponentFilFlytting> logger) : BackgroundService
{
    private const int Porsjon = 20;
    private const long Margin = 200_000_000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            List<string> tilkoblinger = [];
            try
            {
                using var scope = scopeFactory.CreateScope();
                var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
                tilkoblinger = await platformDb.Tenants
                    .Where(t => t.Status == TenantStatus.Aktiv)
                    .Select(t => t.ConnectionString)
                    .ToListAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Filflytting: klarte ikke å hente organisasjonene.");
            }

            foreach (var tilkobling in tilkoblinger)
            {
                try
                {
                    await FlyttAsync(tilkobling, logger, stoppingToken);
                    if (ErNatt())
                    {
                        await KrympAsync(tilkobling, logger, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Filflytting feilet - prøver igjen om en time.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static bool ErNatt()
    {
        DateTime na;
        try
        {
            na = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Oslo");
        }
        catch (Exception)
        {
            na = DateTime.Now;
        }
        return na.Hour is >= 1 and < 5;
    }

    // Flytter alle filer som ligger i databasen til disk. Returnerer antall flyttet.
    public static async Task<int> FlyttAsync(string tilkobling, ILogger logger, CancellationToken ct = default)
    {
        if (KomponentFilLager.Mappe(tilkobling) is not { } mappe)
        {
            return 0;
        }

        await using var con = new SqliteConnection(tilkobling);
        await con.OpenAsync(ct);
        await Kjor(con, "PRAGMA busy_timeout = 10000;", ct);
        if (!await HarTabellAsync(con, ct))
        {
            return 0;
        }

        var flyttet = 0;
        foreach (var kolonne in KomponentFilLager.Kolonner)
        {
            var hoppOver = new HashSet<long>();
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var ider = new List<long>();
                await using (var cmd = con.CreateCommand())
                {
                    // typeof() leser bare posthodet, ikke selve fila.
                    cmd.CommandText = $"SELECT \"Id\" FROM \"Components\" WHERE typeof(\"{kolonne}\") <> 'null' ORDER BY \"Id\" LIMIT {Porsjon + hoppOver.Count}";
                    await using var r = await cmd.ExecuteReaderAsync(ct);
                    while (await r.ReadAsync(ct))
                    {
                        var id = r.GetInt64(0);
                        if (!hoppOver.Contains(id))
                        {
                            ider.Add(id);
                        }
                    }
                }
                if (ider.Count == 0)
                {
                    break;
                }

                foreach (var id in ider)
                {
                    byte[]? data;
                    await using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT \"{kolonne}\" FROM \"Components\" WHERE \"Id\" = $id";
                        cmd.Parameters.AddWithValue("$id", id);
                        await using var r = await cmd.ExecuteReaderAsync(ct);
                        data = await r.ReadAsync(ct) && !r.IsDBNull(0) ? r.GetFieldValue<byte[]>(0) : null;
                    }
                    if (data is null)
                    {
                        continue;
                    }

                    if (KomponentFilLager.LedigPlass(mappe) is long ledig && ledig < data.Length + Margin)
                    {
                        logger.LogWarning("Filflytting stoppet: bare {Ledig} MB ledig på disken. Prøver igjen om en time.", ledig / 1_000_000);
                        return flyttet;
                    }

                    var tmp = KomponentFilLager.SkrivMidlertidig(mappe, (int)id, kolonne, data);
                    var gjeldende = false;
                    try
                    {
                        lock (KomponentFilLager.Las)
                        {
                            using var cmd = con.CreateCommand();
                            cmd.CommandText = $"UPDATE \"Components\" SET \"{kolonne}\" = NULL WHERE \"Id\" = $id AND typeof(\"{kolonne}\") <> 'null' AND length(\"{kolonne}\") = $len";
                            cmd.Parameters.AddWithValue("$id", id);
                            cmd.Parameters.AddWithValue("$len", data.Length);
                            if (cmd.ExecuteNonQuery() == 1)
                            {
                                File.Move(tmp, KomponentFilLager.Sti(mappe, (int)id, kolonne), overwrite: true);
                                gjeldende = true;
                            }
                        }
                    }
                    finally
                    {
                        if (!gjeldende)
                        {
                            KomponentFilLager.SlettStille(tmp);
                        }
                    }

                    if (gjeldende)
                    {
                        flyttet++;
                    }
                    else
                    {
                        // Endret mens vi leste - tas neste runde.
                        hoppOver.Add(id);
                    }
                }

                // Gi portalen pusterom mellom porsjonene.
                await Task.Delay(100, ct);
            }
        }

        if (flyttet > 0)
        {
            logger.LogInformation("Filflytting: {Antall} filer på varene flyttet fra databasen til disk ({Mappe}).", flyttet, mappe);
        }
        return flyttet;
    }

    // Krymper databasen når mye av den er tom plass etter flyttingen.
    // VACUUM trenger like mye ledig plass som databasen er stor.
    public static async Task<bool> KrympAsync(string tilkobling, ILogger logger, CancellationToken ct = default, long minstLedigeSider = 50_000_000)
    {
        var fil = new SqliteConnectionStringBuilder(tilkobling).DataSource;
        if (KomponentFilLager.Mappe(tilkobling) is null || !File.Exists(fil))
        {
            return false;
        }

        await using var con = new SqliteConnection(tilkobling);
        await con.OpenAsync(ct);
        var tom = await Tall(con, "PRAGMA freelist_count;", ct) * await Tall(con, "PRAGMA page_size;", ct);
        if (tom < minstLedigeSider)
        {
            return false;
        }

        var storrelse = new FileInfo(fil).Length;
        if (KomponentFilLager.LedigPlass(Path.GetDirectoryName(Path.GetFullPath(fil))!) is long ledig && ledig < storrelse + Margin)
        {
            logger.LogWarning("Krymping av databasen hoppet over: trenger ca. {Behov} MB ledig, bare {Ledig} MB.", (storrelse + Margin) / 1_000_000, ledig / 1_000_000);
            return false;
        }

        await Kjor(con, "PRAGMA busy_timeout = 30000;", ct);
        await using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "VACUUM;";
            cmd.CommandTimeout = 1800;
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await Kjor(con, "PRAGMA wal_checkpoint(TRUNCATE);", ct);
        logger.LogInformation("Databasen krympet fra {Fra} MB til {Til} MB.", storrelse / 1_000_000, new FileInfo(fil).Length / 1_000_000);
        return true;
    }

    private static async Task<bool> HarTabellAsync(SqliteConnection con, CancellationToken ct) =>
        await Tall(con, "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'Components';", ct) > 0;

    private static async Task<long> Tall(SqliteConnection con, string sql, CancellationToken ct)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct) ?? 0L);
    }

    private static async Task Kjor(SqliteConnection con, string sql, CancellationToken ct)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
