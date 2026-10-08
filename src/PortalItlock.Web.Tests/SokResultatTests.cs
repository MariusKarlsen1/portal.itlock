using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Regresjonstester for søkefiksen i SokResultat.razor (CODE_REVIEW.md): søket
// skal fortsatt finne nøyaktig de samme treffene som før (inkl. æøå,
// uavhengig av store/små bokstaver), bare filtrert i SQL via
// ApplicationDbContext.InneholderUavhengigAvStorForbokstav i stedet for at
// hele tabellen lastes til minnet ved hvert søk.
public class SokResultatTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        connection.Open();
        // Siden tilkoblingen her åpnes manuelt (nødvendig for at SQLite sin
        // :memory:-database skal overleve på tvers av flere operasjoner i
        // samme test), utløses ALDRI interceptoren sin ConnectionOpened-
        // hendelse (den kjører kun når EF Core selv åpner tilkoblingen, som i
        // den ekte DI-injiserte ApplicationDbContext i Program.cs) - må derfor
        // registrere funksjonen manuelt her, med nøyaktig samme logikk.
        PortalItlock.Web.Data.SqliteUnicodeFunctionsInterceptor.RegistrerFunksjoner(connection);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Sok_finner_delvis_treff_uavhengig_av_store_sma_bokstaver_inkludert_aeoa()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.Kunder.AddRange(
                new Kunde { Navn = "Hansen Låsesmed AS" },
                new Kunde { Navn = "Olsen Bygg" });
            await db.SaveChangesAsync();

            var treff = await db.Kunder
                .Where(k => ApplicationDbContext.InneholderUavhengigAvStorForbokstav(k.Navn, "LÅSE"))
                .ToListAsync();

            Assert.Single(treff);
            Assert.Equal("Hansen Låsesmed AS", treff[0].Navn);
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task Sok_med_prosent_eller_understrek_i_sokeordet_matcher_bokstavelig_ikke_som_jokertegn()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.Kunder.AddRange(
                new Kunde { Navn = "Rabatt 50% AS" },
                new Kunde { Navn = "a_b testfirma" },
                new Kunde { Navn = "Helt annet firma" });
            await db.SaveChangesAsync();

            var prosentTreff = await db.Kunder
                .Where(k => ApplicationDbContext.InneholderUavhengigAvStorForbokstav(k.Navn, "50%"))
                .ToListAsync();
            Assert.Single(prosentTreff);
            Assert.Equal("Rabatt 50% AS", prosentTreff[0].Navn);

            var understrekTreff = await db.Kunder
                .Where(k => ApplicationDbContext.InneholderUavhengigAvStorForbokstav(k.Navn, "a_b"))
                .ToListAsync();
            Assert.Single(understrekTreff);
            Assert.Equal("a_b testfirma", understrekTreff[0].Navn);
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task Sok_gir_ingen_treff_pa_null_felt_uten_a_kaste_feil()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.Kunder.Add(new Kunde { Navn = "Testfirma", Epost = null });
            await db.SaveChangesAsync();

            var treff = await db.Kunder
                .Where(k => ApplicationDbContext.InneholderUavhengigAvStorForbokstav(k.Epost, "noe"))
                .ToListAsync();

            Assert.Empty(treff);
        }
        finally
        {
            connection.Close();
        }
    }
}
