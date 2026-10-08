using Microsoft.Data.Sqlite;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Regresjonstest for ytelsesfiksen i TenantStatistikkService (CODE_REVIEW.md,
// Plattform.razor): HentAsync skal gi nøyaktig samme svar som den eksisterende
// synkrone Hent(), bare uten å blokkere en trådpool-tråd.
public class TenantStatistikkServiceTests
{
    private static string LagTestDb()
    {
        var connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
        using var holder = new SqliteConnection(connectionString);
        holder.Open();

        using var cmd = holder.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Brukere (Id INTEGER PRIMARY KEY, Rolle INTEGER);
            CREATE TABLE Prosjekter (Id INTEGER PRIMARY KEY);
            CREATE TABLE Nokkelsystemer (Id INTEGER PRIMARY KEY);
            CREATE TABLE KundeDokumenter (Id INTEGER PRIMARY KEY);
            CREATE TABLE SystemVedlegg (Id INTEGER PRIMARY KEY);
            CREATE TABLE ProsjektVedlegg (Id INTEGER PRIMARY KEY);
            INSERT INTO Brukere (Rolle) VALUES (1), (1), (3);
            INSERT INTO Prosjekter DEFAULT VALUES;
            INSERT INTO Prosjekter DEFAULT VALUES;
            INSERT INTO Nokkelsystemer DEFAULT VALUES;
            INSERT INTO KundeDokumenter DEFAULT VALUES;
            INSERT INTO SystemVedlegg DEFAULT VALUES;
            INSERT INTO SystemVedlegg DEFAULT VALUES;
            """;
        cmd.ExecuteNonQuery();

        // Holder tilkoblingen "holder" åpen til kallers eget bruk er ferdig -
        // :memory:-databaser med cache=shared forsvinner når siste
        // tilkobling mot den lukkes, så vi returnerer kun connectionString
        // og lar testen selv styre levetiden via egne tilkoblinger.
        return connectionString;
    }

    [Fact]
    public async Task HentAsync_gir_samme_resultat_som_synkron_Hent()
    {
        var connectionString = LagTestDb();
        // Hold én tilkobling åpen i bakgrunnen slik at :memory:-databasen
        // ikke slettes mellom de to kallene under.
        using var holdILive = new SqliteConnection(connectionString);
        holdILive.Open();

        var service = new TenantStatistikkService();

        var synkront = service.Hent(connectionString);
        var asynkront = await service.HentAsync(connectionString);

        Assert.Equal(synkront, asynkront);
        Assert.Equal(2, asynkront.Brukere); // Rolle 3 (Kunde) telles ikke med
        Assert.Equal(2, asynkront.Prosjekter);
        Assert.Equal(1, asynkront.Systemer);
        Assert.Equal(3, asynkront.Dokumenter); // 1 KundeDokument + 2 SystemVedlegg + 0 ProsjektVedlegg
    }

    [Fact]
    public async Task HentAsync_gir_alle_nuller_for_ugyldig_connectionString_i_stedet_for_a_kaste_feil()
    {
        var service = new TenantStatistikkService();

        var resultat = await service.HentAsync("Data Source=/finnes/ikke/i/det/hele/tatt.db;Mode=ReadOnly");

        Assert.Equal(new TenantStatistikkService.TenantTall(0, 0, 0, 0), resultat);
    }
}
