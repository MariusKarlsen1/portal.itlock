using Microsoft.Data.Sqlite;

namespace PortalItlock.Web.Services;

// Teller rader direkte i en organisasjons EGEN database (rå ADO.NET mot
// SQLite-filen via Tenant.ConnectionString) for å vise nøkkeltall på
// plattform-siden - uten å måtte sette opp en full ApplicationDbContext per
// organisasjon bare for en opptelling. Svikter en spørring (f.eks. en gammel,
// ikke migrert database som mangler en tabell), returneres bare 0 for den -
// aldri en feil som velter hele plattformsiden.
public class TenantStatistikkService
{
    public sealed record TenantTall(int Prosjekter, int Brukere, int Dokumenter, int Systemer);

    public TenantTall Hent(string connectionString)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            // Rolle = 3 er BrukerRolle.Kunde - telles ikke med her, de har sin
            // egen lisenstype (KundeTilgang) og telles separat ved behov.
            var brukere = Tell(conn, "SELECT COUNT(*) FROM Brukere WHERE Rolle <> 3");
            var prosjekter = Tell(conn, "SELECT COUNT(*) FROM Prosjekter");
            var systemer = Tell(conn, "SELECT COUNT(*) FROM Nokkelsystemer");
            var dokumenter = Tell(conn, "SELECT COUNT(*) FROM KundeDokumenter")
                + Tell(conn, "SELECT COUNT(*) FROM SystemVedlegg")
                + Tell(conn, "SELECT COUNT(*) FROM ProsjektVedlegg");

            return new TenantTall(prosjekter, brukere, dokumenter, systemer);
        }
        catch
        {
            return new TenantTall(0, 0, 0, 0);
        }
    }

    // Som Hent(), men uten å blokkere en trådpool-tråd under databasekallene
    // (synkron ADO.NET-I/O gjør det) - Plattform.razor kaller denne én gang
    // PER organisasjon ved hvert besøk på admin-dashbordet, se CODE_REVIEW.md
    // 2026-10-08. Ubetydelig med dagens 1 organisasjon, men blir en reell,
    // blokkerende kostnad etter hvert som flere kunder legges til.
    public async Task<TenantTall> HentAsync(string connectionString)
    {
        try
        {
            await using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();

            var brukere = await TellAsync(conn, "SELECT COUNT(*) FROM Brukere WHERE Rolle <> 3");
            var prosjekter = await TellAsync(conn, "SELECT COUNT(*) FROM Prosjekter");
            var systemer = await TellAsync(conn, "SELECT COUNT(*) FROM Nokkelsystemer");
            var dokumenter = await TellAsync(conn, "SELECT COUNT(*) FROM KundeDokumenter")
                + await TellAsync(conn, "SELECT COUNT(*) FROM SystemVedlegg")
                + await TellAsync(conn, "SELECT COUNT(*) FROM ProsjektVedlegg");

            return new TenantTall(prosjekter, brukere, dokumenter, systemer);
        }
        catch
        {
            return new TenantTall(0, 0, 0, 0);
        }
    }

    public int TellKundeTilganger(string connectionString)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            return Tell(conn, "SELECT COUNT(*) FROM Brukere WHERE Rolle = 3");
        }
        catch
        {
            return 0;
        }
    }

    private static int Tell(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = cmd.ExecuteScalar();
            return result switch
            {
                long l => (int)l,
                int i => i,
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<int> TellAsync(SqliteConnection conn, string sql)
    {
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync();
            return result switch
            {
                long l => (int)l,
                int i => i,
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }
}
