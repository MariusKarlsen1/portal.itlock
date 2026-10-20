using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Backup per organisasjon. Hver organisasjon er én SQLite-fil som også
// inneholder bilder og vedlegg, så én kopi er hele portalen deres.
//
// Slik tas en kopi:
// 1. VACUUM INTO lager en konsistent kopi mens portalen er i bruk.
// 2. Kopien kontrolleres med PRAGMA integrity_check.
// 3. Den komprimeres (gzip) og krypteres (AES-256-GCM, se BackupKryptering).
// 4. Den lastes opp til lagringen (Azure eller mappe på serveren).
//
// Kopiene ryddes etter alder: 14 daglige, 8 ukentlige og 12 månedlige.
// Manuelle kopier og kopier tatt før en gjenoppretting beholdes i 60 dager.
public class OrganisasjonBackupService(
    PlatformDbContext db,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogger<OrganisasjonBackupService> logger)
{
    private static readonly SemaphoreSlim Las = new(1, 1);

    private string DataMappe => PlatformConnectionStringHelper.FinnDataMappe(config.GetConnectionString("DefaultConnection")!);

    public IBackupLagring Lagring
    {
        get
        {
            var sas = config["Backup:AzureSasUrl"];
            if (!string.IsNullOrWhiteSpace(sas))
            {
                var http = httpFactory.CreateClient(nameof(AzureBackupLagring));
                http.Timeout = TimeSpan.FromMinutes(30);
                return new AzureBackupLagring(http, sas.Trim());
            }

            var mappe = config["Backup:Mappe"];
            return new LokalBackupLagring(string.IsNullOrWhiteSpace(mappe) ? Path.Combine(DataMappe, "backups") : mappe);
        }
    }

    // Nøkkelen kommer fra Backup:Nokkel (base64, 32 byte). Mangler den, lages
    // en og lagres i datamappen, slik at backup virker fra første dag.
    public bool NokkelFraOppsett => !string.IsNullOrWhiteSpace(config["Backup:Nokkel"]);

    public byte[] Nokkel()
    {
        var fraOppsett = config["Backup:Nokkel"];
        if (!string.IsNullOrWhiteSpace(fraOppsett))
        {
            return Convert.FromBase64String(fraOppsett.Trim());
        }

        var fil = Path.Combine(DataMappe, "backup-nokkel.txt");
        if (File.Exists(fil))
        {
            return Convert.FromBase64String(File.ReadAllText(fil).Trim());
        }

        var ny = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(fil, Convert.ToBase64String(ny));
        return ny;
    }

    private static string? DatabaseFil(string connectionString) =>
        new SqliteConnectionStringBuilder(connectionString).DataSource;

    private string PlattformFil => DatabaseFil(PlatformConnectionStringHelper.AvledFra(config.GetConnectionString("DefaultConnection")!))!;

    // ---------- Ta backup ----------

    public async Task<TenantBackup> TaAsync(Tenant? tenant, BackupType? type = null, string? av = null, CancellationToken ct = default)
    {
        await Las.WaitAsync(ct);
        try
        {
            return await TaUtenLasAsync(tenant, type, av, ct);
        }
        finally
        {
            Las.Release();
        }
    }

    private async Task<TenantBackup> TaUtenLasAsync(Tenant? tenant, BackupType? type, string? av, CancellationToken ct)
    {
        var kilde = tenant is null ? PlattformFil : DatabaseFil(tenant.ConnectionString)!;
        var na = DateTime.Now;
        var typen = type ?? await AutomatiskTypeAsync(tenant?.Id, na);
        var mappe = tenant is null ? "plattform" : $"{tenant.Id}-{OrganisasjonOppsett.Slug(tenant.Navn)}";
        var sti = $"{mappe}/{na:yyyy-MM-dd_HHmmss}-{typen.ToString().ToLowerInvariant()}.db.gz.fkb";
        var lagring = Lagring;

        var backup = new TenantBackup
        {
            TenantId = tenant?.Id,
            ErPlattform = tenant is null,
            Navn = tenant?.Navn ?? "Plattform",
            Tidspunkt = na,
            Type = typen,
            Lagring = lagring.Type,
            Sti = sti,
            Av = av
        };

        var temp = Path.Combine(Path.GetTempPath(), $"fk-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        var kopi = Path.Combine(temp, "kopi.db");
        var kryptert = Path.Combine(temp, "kopi.db.gz.fkb");
        try
        {
            // 1. Konsistent kopi
            await using (var con = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = kilde, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            {
                await con.OpenAsync(ct);
                await using var cmd = con.CreateCommand();
                cmd.CommandText = "VACUUM INTO $mal";
                cmd.Parameters.AddWithValue("$mal", kopi);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            backup.DatabaseStorrelse = new FileInfo(kopi).Length;

            // 2. Kontroll
            var kontroll = await IntegritetAsync(kopi, ct);
            backup.Kontrollert = kontroll == "ok";
            if (!backup.Kontrollert)
            {
                backup.Feil = $"Kontrollen fant feil i kopien: {kontroll}";
            }

            // 3. Komprimer og krypter
            await using (var inn = File.OpenRead(kopi))
            await using (var ut = File.Create(kryptert))
            {
                await KomprimerOgKrypterAsync(inn, ut, Nokkel(), ct);
            }

            backup.Storrelse = new FileInfo(kryptert).Length;
            await using (var fil = File.OpenRead(kryptert))
            {
                backup.Sha256 = Convert.ToHexString(await SHA256.HashDataAsync(fil, ct));
            }

            // 4. Lagre
            await lagring.LagreAsync(sti, kryptert, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backup feilet for {Navn}.", backup.Navn);
            backup.Kontrollert = false;
            backup.Feil = ex.Message;
            backup.Storrelse = 0;
        }
        finally
        {
            SqliteConnection.ClearPool(new SqliteConnection($"Data Source={kopi}"));
            try { Directory.Delete(temp, recursive: true); } catch (IOException) { }
        }

        db.TenantBackuper.Add(backup);
        await db.SaveChangesAsync(ct);
        if (backup.Feil is null)
        {
            await RyddAsync(tenant?.Id, ct);
        }

        return backup;
    }

    private static async Task KomprimerOgKrypterAsync(Stream inn, Stream ut, byte[] nokkel, CancellationToken ct)
    {
        // Komprimer til en midlertidig fil først, så krypteringen kan lese i biter.
        var gzFil = Path.GetTempFileName();
        try
        {
            await using (var gzUt = File.Create(gzFil))
            await using (var gz = new GZipStream(gzUt, CompressionLevel.Optimal))
            {
                await inn.CopyToAsync(gz, ct);
            }

            await using var gzInn = File.OpenRead(gzFil);
            await BackupKryptering.KrypterAsync(gzInn, ut, nokkel, ct);
        }
        finally
        {
            File.Delete(gzFil);
        }
    }

    private static async Task<string> IntegritetAsync(string fil, CancellationToken ct)
    {
        await using var con = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = fil, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        await con.OpenAsync(ct);
        await using var cmd = con.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check";
        return (await cmd.ExecuteScalarAsync(ct))?.ToString() ?? "ukjent";
    }

    // Første kopi i måneden er månedlig, første i uka ukentlig, ellers daglig.
    private async Task<BackupType> AutomatiskTypeAsync(int? tenantId, DateTime na)
    {
        var maned = new DateTime(na.Year, na.Month, 1);
        var uke = na.Date.AddDays(-(((int)na.DayOfWeek + 6) % 7));
        var tidligere = await db.TenantBackuper.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.ErPlattform == (tenantId == null) && b.Feil == null && b.Tidspunkt >= (uke < maned ? uke : maned))
            .Select(b => new { b.Type, b.Tidspunkt })
            .ToListAsync();

        if (!tidligere.Any(b => b.Type == BackupType.Manedlig && b.Tidspunkt >= maned))
        {
            return BackupType.Manedlig;
        }

        return tidligere.Any(b => b.Type is BackupType.Ukentlig or BackupType.Manedlig && b.Tidspunkt >= uke)
            ? BackupType.Daglig
            : BackupType.Ukentlig;
    }

    private async Task RyddAsync(int? tenantId, CancellationToken ct)
    {
        var alle = await db.TenantBackuper
            .Where(b => b.TenantId == tenantId && b.ErPlattform == (tenantId == null))
            .OrderByDescending(b => b.Tidspunkt)
            .ToListAsync(ct);

        var slett = new List<TenantBackup>();
        void Behold(BackupType type, int antall) =>
            slett.AddRange(alle.Where(b => b.Type == type && b.Feil == null).Skip(antall));

        Behold(BackupType.Daglig, 14);
        Behold(BackupType.Ukentlig, 8);
        Behold(BackupType.Manedlig, 12);
        var grense = DateTime.Now.AddDays(-60);
        slett.AddRange(alle.Where(b => b.Type is BackupType.Manuell or BackupType.ForGjenoppretting && b.Tidspunkt < grense));
        // Mislykkede forsøk har ingen fil; behold dem en uke så de synes.
        slett.AddRange(alle.Where(b => b.Feil != null && b.Tidspunkt < DateTime.Now.AddDays(-7)));

        foreach (var b in slett.Distinct())
        {
            try
            {
                if (b.Storrelse > 0)
                {
                    await LagringFor(b).SlettAsync(b.Sti, ct);
                }

                db.TenantBackuper.Remove(b);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Klarte ikke å slette gammel backup {Sti}.", b.Sti);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private IBackupLagring LagringFor(TenantBackup b)
    {
        var lagring = Lagring;
        if (lagring.Type == b.Lagring)
        {
            return lagring;
        }

        // Ligger i den andre lagringen (oppsettet er endret siden).
        if (b.Lagring == BackupLagringType.Lokal)
        {
            var mappe = config["Backup:Mappe"];
            return new LokalBackupLagring(string.IsNullOrWhiteSpace(mappe) ? Path.Combine(DataMappe, "backups") : mappe);
        }

        throw new InvalidOperationException("Kopien ligger i Azure, men Azure er ikke satt opp lenger.");
    }

    // ---------- Hente ut og gjenopprette ----------

    // Henter, dekrypterer og pakker ut til en .db-fil. Kalleren sletter filen.
    public async Task<string> HentDatabaseAsync(TenantBackup backup, CancellationToken ct = default)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"fk-hent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        var kryptert = Path.Combine(temp, "kopi.fkb");
        var dbFil = Path.Combine(temp, "kopi.db");

        await LagringFor(backup).HentAsync(backup.Sti, kryptert, ct);
        if (backup.Sha256 is { } forventet)
        {
            await using var fil = File.OpenRead(kryptert);
            if (!string.Equals(Convert.ToHexString(await SHA256.HashDataAsync(fil, ct)), forventet, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Kopien er endret siden den ble tatt (sjekksummen stemmer ikke).");
            }
        }

        var gzFil = Path.Combine(temp, "kopi.db.gz");
        await using (var inn = File.OpenRead(kryptert))
        await using (var ut = File.Create(gzFil))
        {
            await BackupKryptering.DekrypterAsync(inn, ut, Nokkel(), ct);
        }

        await using (var gzInn = File.OpenRead(gzFil))
        await using (var gz = new GZipStream(gzInn, CompressionMode.Decompress))
        await using (var ut = File.Create(dbFil))
        {
            await gz.CopyToAsync(ut, ct);
        }

        File.Delete(kryptert);
        File.Delete(gzFil);
        return dbFil;
    }

    // Setter organisasjonen tilbake til kopien. Tar først en ny kopi av
    // dagens data, så gjenopprettingen kan angres.
    public async Task<string?> GjenopprettAsync(int backupId, string? av, CancellationToken ct = default)
    {
        var backup = await db.TenantBackuper.FirstOrDefaultAsync(b => b.Id == backupId, ct);
        if (backup is null || backup.ErPlattform || backup.TenantId is null || backup.Feil is not null)
        {
            return "Denne kopien kan ikke gjenopprettes.";
        }

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == backup.TenantId, ct);
        if (tenant is null)
        {
            return "Organisasjonen finnes ikke lenger.";
        }

        await Las.WaitAsync(ct);
        string? hentet = null;
        try
        {
            var sikring = await TaUtenLasAsync(tenant, BackupType.ForGjenoppretting, av, ct);
            if (sikring.Feil is not null)
            {
                return $"Klarte ikke å ta kopi av dagens data først, så ingenting er endret: {sikring.Feil}";
            }

            hentet = await HentDatabaseAsync(backup, ct);
            var kontroll = await IntegritetAsync(hentet, ct);
            if (kontroll != "ok")
            {
                return $"Kopien er skadet, så ingenting er endret: {kontroll}";
            }

            var mal = DatabaseFil(tenant.ConnectionString)!;
            SqliteConnection.ClearAllPools();
            foreach (var hale in new[] { "-wal", "-shm" })
            {
                if (File.Exists(mal + hale))
                {
                    File.Delete(mal + hale);
                }
            }

            File.Copy(hentet, mal, overwrite: true);

            // En eldre kopi kan mangle nyere tabeller.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(tenant.ConnectionString).Options;
            await using (var tenantDb = new ApplicationDbContext(options))
            {
                await tenantDb.Database.MigrateAsync(ct);
                await tenantDb.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
            }

            db.TenantHendelser.Add(new TenantHendelse
            {
                TenantId = tenant.Id,
                Tekst = $"Gjenopprettet fra backup {backup.Tidspunkt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}",
                Ikon = "server",
                Av = av
            });
            await db.SaveChangesAsync(ct);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gjenoppretting feilet for {Organisasjon}.", tenant.Navn);
            return $"Gjenopprettingen feilet: {ex.Message}";
        }
        finally
        {
            Las.Release();
            if (hentet is not null)
            {
                try { Directory.Delete(Path.GetDirectoryName(hentet)!, recursive: true); } catch (IOException) { }
            }
        }
    }
}

// Tar backup av alle aktive organisasjoner og plattformdatabasen hver natt
// etter kl. 02, én gang per døgn.
public class OrganisasjonBackupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OrganisasjonBackupBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.Now.Hour >= 2)
                {
                    await KjorAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil i nattlig backup.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task KjorAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var backup = scope.ServiceProvider.GetRequiredService<OrganisasjonBackupService>();
        var iDag = DateTime.Today;

        // Ferdig for i dag når én automatisk kopi har lykkes, eller tre forsøk har feilet.
        bool HarIDag(int? tenantId)
        {
            var iDagsForsok = db.TenantBackuper.Where(b =>
                b.TenantId == tenantId && b.ErPlattform == (tenantId == null) && b.Tidspunkt >= iDag
                && b.Type != BackupType.Manuell && b.Type != BackupType.ForGjenoppretting)
                .Select(b => b.Feil).ToList();
            return iDagsForsok.Any(f => f == null) || iDagsForsok.Count >= 3;
        }

        if (!HarIDag(null))
        {
            await backup.TaAsync(null, ct: ct);
        }

        var organisasjoner = await db.Tenants.AsNoTracking().Where(t => t.Status != TenantStatus.Oppsagt).ToListAsync(ct);
        foreach (var tenant in organisasjoner)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (!HarIDag(tenant.Id))
            {
                await backup.TaAsync(tenant, ct: ct);
            }
        }
    }
}
