using PortalItlock.Web.Models;
using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

// Arbeidsordre direkte på ordren (uten tilbud): varer med pris og rabatt på
// ordren, timer og kjøring som føres kommer med som linjer, og alt kan overstyres.
public class ArbeidsordreDirekteTests
{
    private static readonly List<PrisKode> Koder =
    [
        new() { Id = 1, Kode = "TA100", Navn = "Timer montør", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, ErMontasje = true, Kostpris = 500, Salgspris = 990, Aktiv = true },
    ];

    private static Arbeidsordre Ordre() => new()
    {
        Tittel = "Test",
        ErDirekte = true,
        Timepris = 1090,
        Timeregistreringer =
        [
            new Timeregistrering { Start = TimeSpan.FromHours(8), Slutt = TimeSpan.FromHours(12.5), Fakturerbar = true },
            new Timeregistrering { Start = TimeSpan.FromHours(8), Slutt = TimeSpan.FromHours(10), Fakturerbar = true },
            new Timeregistrering { Start = TimeSpan.FromHours(8), Slutt = TimeSpan.FromHours(9), Fakturerbar = false },
        ],
        KodeLinjer = [new ArbeidsordreKodeLinje { Kode = "OPM100", Navn = "Kjøring", Antall = 1, Kostpris = 1200, Salgspris = 1500 }],
        Varer = [new ArbeidsordreVare { Navn = "ST550", Antall = 4, Kostpris = 2210, Utpris = 3383, Listepris = 3980, RabattProsent = 15 }],
    };

    [Fact]
    public void Forte_timer_og_kjoring_blir_linjer_med_ordrens_timepris()
    {
        var a = ArbeidsordreOkonomiBeregner.DirekteArbeidLinjer(Ordre(), Koder);
        var timer = Assert.Single(a, l => l.Kode == "TA100");
        Assert.Equal(6.5m, timer.Antall);
        Assert.Equal(1090m, timer.Utpris);
        Assert.Equal(7085m, timer.Sum);
        Assert.Equal(1500m, Assert.Single(a, l => l.Kode == "OPM100").Sum);
    }

    [Fact]
    public void Overstyring_av_antall_pris_og_rabatt()
    {
        var o = Ordre();
        o.ArbeidOverstyringJson = ArbeidsordreOkonomiBeregner.SkrivOverstyring(new()
        {
            ["TA100"] = new() { Antall = 5, Utpris = 1000, Rabatt = 10 },
        });
        var timer = Assert.Single(ArbeidsordreOkonomiBeregner.DirekteArbeidLinjer(o, Koder), l => l.Kode == "TA100");
        Assert.True(timer.Overstyrt);
        Assert.Equal(6.5m, timer.FortAntall);
        Assert.Equal(4500m, timer.Sum);
    }

    [Fact]
    public void Fakturalinjene_har_varene_forst_og_arbeidet_etter()
    {
        var linjer = ArbeidsordreOkonomiBeregner.BeregnLinjer(Ordre(), Koder);
        Assert.Equal("ST550", linjer[0].Navn);
        Assert.Equal(4 * 3383m, linjer[0].Belop);
        Assert.Equal("TA100", linjer[1].Varenummer);
        Assert.Equal(4 * 3383m + 7085m + 1500m, linjer.Sum(l => l.Belop));
    }

    [Fact]
    public void Ordre_fra_tilbud_fakturerer_ikke_forte_timer()
    {
        var o = Ordre();
        o.ErDirekte = false;
        o.Tilbud = new Tilbud { Tittel = "T", Linjer = [], KodeLinjer = [] };
        var linjer = ArbeidsordreOkonomiBeregner.BeregnLinjer(o, Koder);
        Assert.DoesNotContain(linjer, l => l.Varenummer == "TA100");
    }
}
