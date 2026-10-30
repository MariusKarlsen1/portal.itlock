using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Kassa: mva av beløp inkl. mva, summene i dagsoppgjøret (med kreditnota)
// og lesing av kontoutskrift til bankavstemmingen.
public class KassaServiceTests
{
    [Theory]
    [InlineData(1250, 25, 250)]
    [InlineData(149, 25, 29.80)]
    [InlineData(115, 15, 15)]
    [InlineData(100, 0, 0)]
    public void MvaAv_RegnerMvaAvBelopInklMva(decimal belop, decimal sats, decimal forventet) =>
        Assert.Equal(forventet, KassaService.MvaAv(belop, sats));

    [Fact]
    public void Summer_TrekkerKreditnotaFraRiktigBetalingsmate()
    {
        var salg = new List<KasseSalg>
        {
            new() { Nummer = 1001, Type = KasseSalgType.Kort, Total = 1000, Mva = 200 },
            new() { Nummer = 1002, Type = KasseSalgType.Kontant, Total = 500, Mva = 100 },
            new() { Nummer = 1003, Type = KasseSalgType.Kreditnota, BetaltTilbakeMed = KasseSalgType.Kontant, Total = -100, Mva = -20 },
        };

        var t = KassaService.Summer(salg);

        Assert.Equal(2, t.AntallSalg);
        Assert.Equal(1, t.AntallKreditnota);
        Assert.Equal(1400, t.Omsetning);
        Assert.Equal(1000, t.NettoKort);
        Assert.Equal(400, t.NettoKontant);
        Assert.Equal(1001, t.FraNummer);
        Assert.Equal(1003, t.TilNummer);
    }

    [Fact]
    public void MvaFordeling_GrupperPerSats()
    {
        var linjer = new List<KasseSalgLinje>
        {
            new() { Navn = "A", MvaSats = 25, Belop = 125, Mva = 25 },
            new() { Navn = "B", MvaSats = 25, Belop = 250, Mva = 50 },
            new() { Navn = "C", MvaSats = 15, Belop = 115, Mva = 15 },
        };

        var m = KassaService.MvaFordeling(linjer);

        Assert.Equal(2, m.Count);
        Assert.Equal(300, m[0].Grunnlag);
        Assert.Equal(75, m[0].Mva);
        Assert.Equal(15, m[1].Sats);
    }

    [Fact]
    public void LesKontoutskrift_LeserNorskFormatOgHopperOverUtbetalinger()
    {
        const string csv = """
            Dato;Forklaring;Rentedato;Ut fra konto;Inn på konto
            29.10.2026;Nets kortoppgjør 28.10;29.10.2026;;3 577,00
            29.10.2026;Husleie;29.10.2026;-12 000,00;
            30.10.2026;"Nexi Norge AS";30.10.2026;;1.234,50
            """;

        var poster = KassaService.LesKontoutskrift(csv);

        Assert.Equal(2, poster.Count);
        Assert.Equal(3577.00m, poster[0].Belop);
        Assert.Equal(new DateTime(2026, 10, 29), poster[0].Dato);
        Assert.Equal(1234.50m, poster[1].Belop);
    }
}
