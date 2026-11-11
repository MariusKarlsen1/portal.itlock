using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Befaring/tilbud på serviceavtaler: dører med bare komponenttyper (plassholdere)
// som prises med prisreglene og spesifiseres senere. Sjekklister per type med
// krav, fotokrav og faste tillegg på enkeltdører.
public class ServiceBefaringTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return db;
    }

    [Theory]
    [InlineData(new[] { "101", "102", "109" }, "110")]
    [InlineData(new[] { "B-1", "B-2" }, "B-3")]
    [InlineData(new string[0], "1")]
    public void NesteDornummer(string[] finnes, string forventet) =>
        Assert.Equal(forventet, ServiceBefaring.NesteDornummer(finnes));

    [Fact]
    public async Task Plassholdere_LagesEnGangOgBrukerEksisterendeType()
    {
        using var db = LagDb(out var c);
        var auto = new ComponentType { Navn = "Dørautomatikk swing" };
        db.ComponentTypes.Add(auto);
        await db.SaveChangesAsync();

        var plass = await ServiceBefaring.SikrePlassholdere(db);
        var igjen = await ServiceBefaring.SikrePlassholdere(db);

        Assert.Equal(ServiceRegelMotor.Servicetyper.Length, plass.Count);
        var typeKart = await ServiceRegelMotor.HentTypeKart(db);
        Assert.Equal("Dørautomatikk", typeKart[plass["Dørautomatikk"].ComponentTypeId!.Value]);
        Assert.Equal("Dørvrider", typeKart[plass["Dørvrider"].ComponentTypeId!.Value]);
        Assert.True(plass["Dørautomatikk"].ErPlassholder);
        Assert.False(plass["Dørautomatikk"].Aktiv);
        Assert.Equal(plass["Kortleser"].Id, igjen["Kortleser"].Id);
        Assert.Equal(ServiceRegelMotor.Servicetyper.Length, await db.Components.CountAsync(x => x.ErPlassholder));
    }

    [Fact]
    public async Task DorMedBareTyper_KlassifiseresMedPrisreglene()
    {
        using var db = LagDb(out var c);
        await ServiceRegelMotor.SikreStandardregler(db);
        var plass = await ServiceBefaring.SikrePlassholdere(db);
        var p = new Prosjekt { Navn = "Gammelt bygg", Status = ProsjektStatus.Serviceavtale };
        var d = new Dor { Dornummer = "101", Prosjekt = p, Komponenter = [new DorKomponent { ComponentId = plass["Dørautomatikk"].Id }, new DorKomponent { ComponentId = plass["Dørvrider"].Id }] };
        db.AddRange(p, d);
        await db.SaveChangesAsync();

        var dorer = await ServiceRegelMotor.HentDorer(db, p.Id);
        var res = ServiceRegelMotor.Klassifiser(dorer[0], await ServiceRegelMotor.HentRegler(db), await ServiceRegelMotor.HentTypeKart(db));

        Assert.Equal(ServiceKategori.Automatikk, res.Kategori);
        Assert.Equal(["Dørautomatikk", "Dørvrider"], ServiceBefaring.UspesifiserteTyper(dorer[0], await ServiceRegelMotor.HentTypeKart(db)).Order());
    }

    [Fact]
    public async Task Spesifiser_ByttePlassholderMedProdukt()
    {
        using var db = LagDb(out var c);
        var plass = await ServiceBefaring.SikrePlassholdere(db);
        var produkt = new Component { Navn = "dormakaba ED100", ComponentTypeId = plass["Dørautomatikk"].ComponentTypeId };
        var p = new Prosjekt { Navn = "Gammelt bygg", Status = ProsjektStatus.Serviceavtale };
        var d1 = new Dor { Dornummer = "101", Prosjekt = p, Komponenter = [new DorKomponent { ComponentId = plass["Dørautomatikk"].Id }] };
        var d2 = new Dor { Dornummer = "102", Prosjekt = p, Komponenter = [new DorKomponent { ComponentId = plass["Dørautomatikk"].Id }] };
        db.AddRange(produkt, p, d1, d2);
        await db.SaveChangesAsync();

        var antall = await ServiceBefaring.Spesifiser(db, [d1.Id], "Dørautomatikk", produkt.Id);
        db.ChangeTracker.Clear();

        Assert.Equal(1, antall);
        var k1 = await db.DorKomponenter.Include(k => k.Component).SingleAsync(k => k.DorId == d1.Id);
        Assert.Equal(produkt.Id, k1.ComponentId);
        Assert.True((await db.DorKomponenter.Include(k => k.Component).SingleAsync(k => k.DorId == d2.Id)).Component!.ErPlassholder);
        var produkter = await ServiceBefaring.ProdukterFor(db, "Dørautomatikk", null);
        Assert.Equal(produkt.Id, Assert.Single(produkter).Id);
    }

    [Fact]
    public void PunkterForDor_TarMedKravFotoOgTilleggPaDoren()
    {
        List<ServiceKontrollpunkt> mal =
        [
            new() { Servicetype = "Dørautomatikk", Tekst = "Sikkerhetssensorer", Standard = "NS-EN 16005", KreverFoto = true, Rekkefolge = 0 },
            new() { Servicetype = "Dørlukker", Tekst = "Lukker helt", Rekkefolge = 0 },
            new() { Servicetype = "Dør", Tekst = "Ekstra test av nattlås", DorId = 7, Rekkefolge = 0 },
            new() { Servicetype = "Dør", Tekst = "Punkt på en annen dør", DorId = 8, Rekkefolge = 0 },
        ];

        var p = ServicerundeMotor.PunkterForDor(["Dørautomatikk", "Dørlukker"], mal, 7);

        Assert.Equal(3, p.Count);
        var sensor = p.Single(x => x.Tekst == "Sikkerhetssensorer");
        Assert.Equal("NS-EN 16005", sensor.Standard);
        Assert.True(sensor.KreverFoto);
        Assert.Equal("Denne døren", p.Single(x => x.Tekst == "Ekstra test av nattlås").Kilde);
        Assert.DoesNotContain(p, x => x.Tekst == "Punkt på en annen dør");
    }
}
