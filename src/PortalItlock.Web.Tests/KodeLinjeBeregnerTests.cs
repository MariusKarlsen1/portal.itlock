using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Kodelinjene på tilbudet: kroner, prosent av varer og prosent av arbeid,
// og at totalene skrives riktig til tilbudets montasjefelt.
public class KodeLinjeBeregnerTests
{
    private static TilbudKodeLinje L(string kode, PrisKodeType type, PrisKodeEnhet enhet, decimal antall, decimal kost, decimal salg) =>
        new() { Kode = kode, Navn = kode, Type = type, Enhet = enhet, Antall = antall, Kostpris = kost, Salgspris = salg };

    private static List<TilbudKodeLinje> Eksempel() =>
    [
        L("100", PrisKodeType.Arbeid, PrisKodeEnhet.Time, 280, 550, 950),
        L("120", PrisKodeType.Arbeid, PrisKodeEnhet.Time, 16, 600, 1150),
        L("200", PrisKodeType.Kjoring, PrisKodeEnhet.Km, 1400, 4.5m, 9),
        L("300", PrisKodeType.Materiell, PrisKodeEnhet.ProsentVarer, 1, 2, 4),
        L("400", PrisKodeType.Tillegg, PrisKodeEnhet.ProsentArbeid, 1, 0, 5),
    ];

    [Fact]
    public void Prosentlinjer_RegnesAvVarerOgArbeid()
    {
        var linjer = Eksempel();

        Assert.Equal(new KodeLinjeBeregner.Linjesum(10400, 20800), KodeLinjeBeregner.Sum(linjer[3], linjer, 520000));
        // 5 % av arbeidet: (266 000 + 18 400) × 5 %.
        Assert.Equal(14220m, KodeLinjeBeregner.Sum(linjer[4], linjer, 520000).Salg);
    }

    [Fact]
    public void Totalt_SummererAlleLinjer()
    {
        var tot = KodeLinjeBeregner.Totalt(Eksempel(), 520000);

        Assert.Equal(296m, tot.Timer);
        Assert.Equal(332020m, tot.Salg);
        // 154 000 + 9 600 + 6 300 + 10 400 + 0
        Assert.Equal(180300m, tot.Kost);
    }

    [Fact]
    public void Synk_SkriverTilMontasjefeltene()
    {
        var t = new Tilbud { Tittel = "T", BrukerKodelinjer = true, RiggDriftProsent = 10, KodeLinjer = Eksempel() };

        KodeLinjeBeregner.Synk(t, 520000);

        Assert.Equal(0m, t.RiggDriftProsent);
        Assert.Equal(296m, t.EstimertTimerOverride);
        Assert.Equal(332020m, t.Montasjekost);
        // Montasjekost i kroner = MontasjeInnpris × timer.
        Assert.Equal(180300m, Math.Round(t.MontasjeInnpris!.Value * 296, 0));
        Assert.Equal(Math.Round((266000m + 18400m) / 296, 2), t.Timepris);
    }

    [Fact]
    public void Synk_GjorIngentingUtenKodelinjer()
    {
        var t = new Tilbud { Tittel = "T", Timepris = 900, EstimertTimerOverride = 10, KodeLinjer = Eksempel() };

        KodeLinjeBeregner.Synk(t, 520000);

        Assert.Equal(900m, t.Timepris);
        Assert.Equal(10m, t.EstimertTimerOverride);
    }
}
