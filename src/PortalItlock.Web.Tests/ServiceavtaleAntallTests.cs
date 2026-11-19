using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Tilbud serviceavtale: antall per kategori × pris, tillegg, runder og rabatt.
public class ServiceavtaleAntallTests
{
    private static ServiceavtaleLinje Kat(string navn, decimal pris, decimal antall) =>
        new() { Navn = navn, Pris = pris, Antall = antall };

    private static ServiceavtaleLinje Till(string type, decimal pris, decimal antall = 1) =>
        new() { Navn = type, ErTillegg = true, TilleggType = type, Pris = pris, Antall = antall };

    [Fact]
    public void Kategorier_GangerRunder()
    {
        var p = new Prosjekt { Navn = "A", ServiceGangerPerAr = 2 };
        var b = ServiceavtaleAntall.Beregn(p, [Kat("Dørautomatikk", 1250, 4), Kat("Mekanisk", 350, 12), Kat("Adgangskontroll", 900, 6), Kat("Tom", 600, 0)]);
        Assert.Equal(14600m, b.PerRunde);
        Assert.Equal(29200m, b.Arspris);
        Assert.Equal(36500m, b.InklMva);
    }

    [Fact]
    public void Tillegg_PerRundeKmArOgProsent()
    {
        var p = new Prosjekt { Navn = "A", ServiceGangerPerAr = 2 };
        var b = ServiceavtaleAntall.Beregn(p,
        [
            Kat("Mekanisk", 500, 20),                          // 10 000 per runde, 20 000 per år
            Till(ServiceavtaleLinje.PerRunde, 450),             // 900
            Till(ServiceavtaleLinje.PerKm, 8, 60),              // 960
            Till(ServiceavtaleLinje.PerAr, 1000),               // 1 000
            Till(ServiceavtaleLinje.Prosent, 5),                // 5 % av 20 000 = 1 000
            Till(ServiceavtaleLinje.PerRunde, 0),               // tom linje teller ikke
        ]);
        Assert.Equal(20000m, b.Kategorier);
        Assert.Equal(3860m, b.Tillegg);
        Assert.Equal(23860m, b.Arspris);
    }

    [Fact]
    public void Rabatt_TrekkesFraAlt()
    {
        var p = new Prosjekt { Navn = "A", ServiceGangerPerAr = 1, ServiceRabattProsent = 10 };
        var b = ServiceavtaleAntall.Beregn(p, [Kat("Mekanisk", 1000, 10), Till(ServiceavtaleLinje.PerRunde, 1000)]);
        Assert.Equal(11000m, b.Brutto);
        Assert.Equal(1100m, b.Rabatt);
        Assert.Equal(9900m, b.Arspris);
    }

    [Fact]
    public void Flett_FyllerInnOgKoderHtml()
    {
        var p = new Prosjekt { Navn = "Bygg <A>", Adresse = "Gata 1", ServiceGangerPerAr = 4, Kunde = new Kunde { Navn = "Kunde & Co" } };
        var b = ServiceavtaleAntall.Beregn(p, [Kat("Mekanisk", 250, 10)]);
        var t = ServiceavtaleAntall.Flett("<p>{kunde} – {bygg}, {adresse}: {runder} runder, {pris}</p>", p, b);
        Assert.Equal("<p>Kunde &amp; Co – Bygg &lt;A&gt;, Gata 1: 4 runder, 10 000 kr</p>", t.Replace(' ', ' '));
    }

    [Fact]
    public void Valg_StandardOgLesing()
    {
        Assert.Contains(ServiceavtaleAntall.VisLinjer, ServiceavtaleAntall.Valg(new Prosjekt { Navn = "A" }));
        Assert.Equal(["mnd"], ServiceavtaleAntall.Valg(new Prosjekt { Navn = "A", ServiceTilbudValg = " mnd ," }));
    }
}
