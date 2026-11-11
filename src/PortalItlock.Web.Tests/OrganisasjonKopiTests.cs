using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Kopiering av grunndata og prosjekter mellom organisasjoner (plattform-admin).
public class OrganisasjonKopiTests : IDisposable
{
    private readonly string _mappe = Path.Combine(Path.GetTempPath(), "kopitest-" + Guid.NewGuid().ToString("N"));

    public OrganisasjonKopiTests() => Directory.CreateDirectory(_mappe);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_mappe, true); } catch (IOException) { }
    }

    private (string Cs, ApplicationDbContext Db) LagDb(string navn)
    {
        var cs = $"Data Source={Path.Combine(_mappe, navn + ".db")}";
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(cs).Options);
        db.Database.EnsureCreated();
        return (cs, db);
    }

    [Fact]
    public async Task KopiererGrunndataOgProsjektMedTilbudOgOrdre()
    {
        var (kildeCs, kilde) = LagDb("kilde");
        var (malCs, mal) = LagDb("mal");

        var lev = new Leverandor { Navn = "ASSA ABLOY Test" };
        var type = new ComponentType { Navn = "Testtype kopi" };
        var komp = new Component { Navn = "Låskasse 565 kopi", Produktkode = "565-K", Type = type };
        var pakke = new Package { Navn = "Kontordør kopi", Komponenter = [new PackageComponent { Component = komp, Antall = 1 }] };
        var kunde = new Kunde { Navn = "Steinerskolen kunde" };
        var p = new Prosjekt { Navn = "Steinerskolen", Kunde = kunde };
        var t = new Tilbud { Prosjekt = p, Tittel = "Tilbud lås", Linjer = [new TilbudLinje { Component = komp, Navn = "Låskasse", Antall = 2 }] };
        var ao = new Arbeidsordre { Prosjekt = p, Tilbud = t, Tittel = "Montering" };
        var d = new Dor { Dornummer = "S-101", Prosjekt = p, Komponenter = [new DorKomponent { Component = komp, Antall = 1 }] };
        kilde.AddRange(lev, type, komp, pakke, kunde, p, t, ao, d);
        kilde.Add(new Prosjekt { Navn = "Annet prosjekt" });
        await kilde.SaveChangesAsync();
        var pid = p.Id;
        await kilde.DisposeAsync();

        // Mottakeren har allerede komponenttypen (skal gjenbrukes, ikke dobles).
        mal.ComponentTypes.Add(new ComponentType { Navn = "Testtype kopi" });
        await mal.SaveChangesAsync();
        await mal.DisposeAsync();

        var res = await OrganisasjonKopiService.KopierUtenBackupAsync(kildeCs, malCs, "kilde",
            ["leverandorer", "komponenter", "dorpakker"], [pid]);
        Assert.True(res.Ok, res.Melding);

        await using var sjekk = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(malCs).Options);
        Assert.Equal(1, await sjekk.ComponentTypes.CountAsync(x => x.Navn == "Testtype kopi"));
        Assert.Equal(1, await sjekk.Components.CountAsync(x => x.Produktkode == "565-K"));
        Assert.True(await sjekk.Leverandorer.AnyAsync(x => x.Navn == "ASSA ABLOY Test"));
        var nyPakke = await sjekk.Packages.Include(x => x.Komponenter).SingleAsync(x => x.Navn == "Kontordør kopi");
        Assert.Single(nyPakke.Komponenter);

        var nyttP = await sjekk.Prosjekter.Include(x => x.Kunde).SingleAsync(x => x.Navn == "Steinerskolen");
        Assert.Equal("Steinerskolen kunde", nyttP.Kunde!.Navn);
        Assert.False(await sjekk.Prosjekter.AnyAsync(x => x.Navn == "Annet prosjekt"));
        var nyttT = await sjekk.Tilbud.Include(x => x.Linjer).SingleAsync(x => x.ProsjektId == nyttP.Id);
        Assert.Single(nyttT.Linjer);
        var nyAo = await sjekk.Arbeidsordre.SingleAsync(x => x.ProsjektId == nyttP.Id);
        Assert.Equal(nyttT.Id, nyAo.TilbudId);
        var nyDor = await sjekk.Dorer.Include(x => x.Komponenter).SingleAsync(x => x.ProsjektId == nyttP.Id);
        Assert.Equal(nyPakke.Komponenter[0].ComponentId, nyDor.Komponenter[0].ComponentId);

        // En gang til: grunndata dobles ikke.
        var igjen = await OrganisasjonKopiService.KopierUtenBackupAsync(kildeCs, malCs, "kilde", ["komponenter", "dorpakker"], []);
        Assert.True(igjen.Ok, igjen.Melding);
        Assert.Equal(1, await sjekk.Components.CountAsync(x => x.Produktkode == "565-K"));
        Assert.Equal(1, await sjekk.Packages.CountAsync(x => x.Navn == "Kontordør kopi"));
    }
}
