using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Flytter store BLOB-kolonner (PDF-er og bilder) bakerst i tabellene.
//
// Hvorfor: SQLite lagrer en rad som én post. Er et felt stort (en FDV-PDF på
// 300 kB), havner det i en kjede av "overflow"-sider, og for å lese en kolonne
// som ligger ETTER det feltet må SQLite gå gjennom hele kjeden - side for side.
// Components hadde FdvData som kolonne 7 av 50, så å liste opp navn/varenummer
// for hele komponentregisteret betydde i praksis å lese alle PDF-ene i
// databasen (målt: 618 ms mot 46 ms for 25 000 varer). Det samme gjaldt bl.a.
// Kunder (Status ligger etter BildeData) og Kunngjoringer (Tittel, Innhold osv.
// ligger etter BildeData). Selve modellen bryr seg ikke om rekkefølgen.
//
// Hvordan: ALTER TABLE ADD COLUMN legger alltid kolonnen bakerst. For hver
// tabell legges en ny kolonne "<navn>__ny" til bakerst, triggere speiler nye
// skrivinger dit, og innholdet kopieres over i små porsjoner mens portalen
// kjører. Til slutt byttes navnene i én kort transaksjon (gammel → "__gml",
// ny → originalnavnet), og den gamle kolonnen tømmes for å frigjøre plass.
// Den tomme "__gml"-kolonnen blir liggende (NULL koster bare én byte i
// posthodet). Alt avledes fra skjemaet ved oppstart, så jobben kan avbrytes
// når som helst (omstart/deploy) og fortsetter der den slapp.
//
// Gjøres i bakgrunnen og ikke som EF-migrering fordi kopieringen tar tid på
// en stor database, og migreringer kjører før appen svarer på forespørsler.
public class BlobKolonneOmorganisering(IServiceScopeFactory scopeFactory, ILogger<BlobKolonneOmorganisering> logger) : BackgroundService
{
    // Bare kolonner som kan være NULL - "__gml"-kolonnen må kunne stå tom igjen.
    private static readonly (string Tabell, string[] Kolonner)[] Mal =
    [
        ("Components", ["FdvData", "MontasjebladData", "DatabladData", "BildeData"]),
        ("Kunder", ["BildeData"]),
        ("Kunngjoringer", ["BildeData"]),
        ("Leverandorer", ["LogoData"]),
        ("Nokkelsystemer", ["BildeData"]),
        ("Fellesskap", ["OmslagData"]),
    ];

    private const int Porsjon = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // La oppstart og første forespørsler gå først.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        List<string> tilkoblinger;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            tilkoblinger = await platformDb.Tenants
                .Where(t => t.Status == TenantStatus.Aktiv)
                .Select(t => t.ConnectionString)
                .ToListAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blob-omorganisering: klarte ikke å hente leietakere.");
            return;
        }

        foreach (var tilkobling in tilkoblinger)
        {
            foreach (var (tabell, kolonner) in Mal)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await OmorganiserAsync(tilkobling, tabell, kolonner, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Feiler noe, står tabellen urørt eller halvveis - begge deler
                    // fungerer for appen, og neste oppstart prøver igjen.
                    logger.LogError(ex, "Blob-omorganisering av {Tabell} feilet - prøver igjen ved neste oppstart.", tabell);
                }
            }
        }
    }

    private async Task OmorganiserAsync(string tilkobling, string tabell, string[] alleKolonner, CancellationToken ct)
    {
        await using var con = new SqliteConnection(tilkobling);
        await con.OpenAsync(ct);
        await KjorAsync(con, "PRAGMA busy_timeout = 30000;", ct);

        var info = await KolonneInfoAsync(con, tabell, ct);
        if (info.Count == 0)
        {
            return;
        }

        // Rydd bort gamle kolonner fra et tidligere bytte (frigjør plass).
        foreach (var gml in info.Keys.Where(k => k.Contains("__gml", StringComparison.Ordinal)).ToList())
        {
            await TomKolonneAsync(con, tabell, gml, ct);
        }

        var blobSett = alleKolonner.ToHashSet(StringComparer.Ordinal);
        var kolonner = alleKolonner
            .Where(k => info.TryGetValue(k, out var i) && !i.NotNull)
            .Where(k => info.ContainsKey(k + "__ny")
                || info.Any(x => x.Value.Cid > info[k].Cid && !blobSett.Contains(x.Key)
                    && !x.Key.EndsWith("__ny", StringComparison.Ordinal) && !x.Key.Contains("__gml", StringComparison.Ordinal)))
            .ToList();
        if (kolonner.Count == 0)
        {
            return;
        }

        logger.LogInformation("Blob-omorganisering: flytter {Kolonner} bakerst i {Tabell}.", string.Join(", ", kolonner), tabell);

        // 1) Nye kolonner bakerst + triggere som speiler skrivinger mens vi kopierer.
        await using (var tx = (SqliteTransaction)await con.BeginTransactionAsync(ct))
        {
            foreach (var k in kolonner.Where(k => !info.ContainsKey(k + "__ny")))
            {
                await KjorAsync(con, $"ALTER TABLE \"{tabell}\" ADD COLUMN \"{k}__ny\" BLOB NULL;", ct, tx);
            }

            var sett = string.Join(", ", kolonner.Select(k => $"\"{k}__ny\" = NEW.\"{k}\""));
            var avhengig = string.Join(", ", kolonner.Select(k => $"\"{k}\""));
            await KjorAsync(con, $"CREATE TRIGGER IF NOT EXISTS \"omorg_{tabell}_u\" AFTER UPDATE OF {avhengig} ON \"{tabell}\" BEGIN UPDATE \"{tabell}\" SET {sett} WHERE \"Id\" = NEW.\"Id\"; END;", ct, tx);
            await KjorAsync(con, $"CREATE TRIGGER IF NOT EXISTS \"omorg_{tabell}_i\" AFTER INSERT ON \"{tabell}\" BEGIN UPDATE \"{tabell}\" SET {sett} WHERE \"Id\" = NEW.\"Id\"; END;", ct, tx);
            await tx.CommitAsync(ct);
        }

        // 2) Kopier i små porsjoner. typeof() leser bare posthodet, ikke selve fila.
        var mangler = string.Join(" OR ", kolonner.Select(k => $"(typeof(\"{k}\") <> 'null' AND typeof(\"{k}__ny\") = 'null')"));
        var kopier = string.Join(", ", kolonner.Select(k => $"\"{k}__ny\" = \"{k}\""));
        var kopiert = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var ider = await IderAsync(con, $"SELECT \"Id\" FROM \"{tabell}\" WHERE {mangler} ORDER BY \"Id\" LIMIT {Porsjon};", ct);
            if (ider.Count == 0)
            {
                break;
            }

            await KjorAsync(con, $"UPDATE \"{tabell}\" SET {kopier} WHERE \"Id\" IN ({string.Join(",", ider)});", ct);
            kopiert += ider.Count;
            await Task.Delay(25, ct);
        }

        // 3) Bytt navn i én kort transaksjon, etter en siste kontroll.
        List<string> gamle;
        await using (var tx = (SqliteTransaction)await con.BeginTransactionAsync(ct))
        {
            var rest = await IderAsync(con, $"SELECT \"Id\" FROM \"{tabell}\" WHERE {mangler} LIMIT 1;", ct, tx);
            if (rest.Count > 0)
            {
                await tx.RollbackAsync(ct);
                logger.LogWarning("Blob-omorganisering av {Tabell}: nye rader under kopiering - prøver igjen ved neste oppstart.", tabell);
                return;
            }

            await KjorAsync(con, $"DROP TRIGGER IF EXISTS \"omorg_{tabell}_u\";", ct, tx);
            await KjorAsync(con, $"DROP TRIGGER IF EXISTS \"omorg_{tabell}_i\";", ct, tx);
            gamle = [];
            foreach (var k in kolonner)
            {
                // Har kolonnen vært flyttet før (en senere migrering la nye
                // kolonner bak den), finnes "__gml" allerede - velg ledig navn.
                var gml = k + "__gml";
                for (var n = 2; info.ContainsKey(gml); n++)
                {
                    gml = $"{k}__gml{n}";
                }

                gamle.Add(gml);
                await KjorAsync(con, $"ALTER TABLE \"{tabell}\" RENAME COLUMN \"{k}\" TO \"{gml}\";", ct, tx);
                await KjorAsync(con, $"ALTER TABLE \"{tabell}\" RENAME COLUMN \"{k}__ny\" TO \"{k}\";", ct, tx);
            }

            await tx.CommitAsync(ct);
        }

        // 4) Tøm de gamle kolonnene.
        foreach (var gml in gamle)
        {
            await TomKolonneAsync(con, tabell, gml, ct);
        }

        logger.LogInformation("Blob-omorganisering av {Tabell} ferdig ({Antall} rader kopiert).", tabell, kopiert);
    }

    private static async Task TomKolonneAsync(SqliteConnection con, string tabell, string kolonne, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var ider = await IderAsync(con, $"SELECT \"Id\" FROM \"{tabell}\" WHERE typeof(\"{kolonne}\") <> 'null' LIMIT {Porsjon};", ct);
            if (ider.Count == 0)
            {
                return;
            }

            await KjorAsync(con, $"UPDATE \"{tabell}\" SET \"{kolonne}\" = NULL WHERE \"Id\" IN ({string.Join(",", ider)});", ct);
            await Task.Delay(25, ct);
        }
    }

    private sealed record Kolonne(int Cid, bool NotNull);

    private static async Task<Dictionary<string, Kolonne>> KolonneInfoAsync(SqliteConnection con, string tabell, CancellationToken ct)
    {
        var resultat = new Dictionary<string, Kolonne>(StringComparer.Ordinal);
        await using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT cid, name, \"notnull\" FROM pragma_table_info($t);";
        cmd.Parameters.AddWithValue("$t", tabell);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            resultat[r.GetString(1)] = new Kolonne(r.GetInt32(0), r.GetInt32(2) == 1);
        }

        return resultat;
    }

    private static async Task<List<long>> IderAsync(SqliteConnection con, string sql, CancellationToken ct, SqliteTransaction? tx = null)
    {
        var ider = new List<long>();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            ider.Add(r.GetInt64(0));
        }

        return ider;
    }

    private static async Task KjorAsync(SqliteConnection con, string sql, CancellationToken ct, SqliteTransaction? tx = null)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        cmd.CommandTimeout = 120;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
