using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Kodene fra tilbud og arbeidsordre til fakturaen (Tripletex-ordren) og
// til prosjektøkonomien: én linje per kode med antall 1 og summen.
public class ArbeidsordreKoderTests
{
    private static readonly List<PrisKode> Koder =
    [
        new() { Id = 1, Kode = "100", Navn = "Montasje", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, Kostpris = 550, Salgspris = 950, ErMontasje = true },
        new() { Id = 2, Kode = "110", Navn = "Overtid", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, Kostpris = 750, Salgspris = 1425 },
        new() { Id = 3, Kode = "200", Navn = "Kjøring", Type = PrisKodeType.Kjoring, Enhet = PrisKodeEnhet.Km, Kostpris = 4.5m, Salgspris = 9 },
    ];

    private static Timeregistrering Timer(decimal timer, int? kode = null, bool fakturerbar = true) =>
        new() { Start = TimeSpan.FromHours(7), Slutt = TimeSpan.FromHours(7) + TimeSpan.FromHours((double)timer), PrisKodeId = kode, Fakturerbar = fakturerbar };

    [Fact]
    public void Tilbud_MedKodelinjer_GirEnLinjePerKode()
    {
        var tilbud = new Tilbud { Tittel = "T", BrukerKodelinjer = true, Montasjekost = 99999 };
        tilbud.KodeLinjer.Add(new() { Kode = "100", Navn = "Montasje", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, Antall = 67, Salgspris = 950 });
        tilbud.KodeLinjer.Add(new() { Kode = "200", Navn = "Kjøring", Type = PrisKodeType.Kjoring, Enhet = PrisKodeEnhet.Km, Antall = 120, Salgspris = 9 });
        var ao = new Arbeidsordre { Tittel = "AO", Tilbud = tilbud };
        // Ført på arbeidsordren: på fastpris er det bare kost, ikke faktura.
        ao.KodeLinjer.Add(new() { Kode = "200", Navn = "Kjøring", Antall = 300, Salgspris = 9 });

        var linjer = ArbeidsordreOkonomiBeregner.BeregnLinjer(ao, Koder);

        Assert.Equal(2, linjer.Count);
        Assert.Equal(("100", 1m, 63650m), (linjer[0].Varenummer, linjer[0].Antall, linjer[0].Belop));
        Assert.Equal(("200", 1m, 1080m), (linjer[1].Varenummer, linjer[1].Antall, linjer[1].Belop));
    }

    [Fact]
    public void UtenTilbud_FakturererTimerPerKodeOgTillegg()
    {
        var ao = new Arbeidsordre { Tittel = "AO" };
        ao.Timeregistreringer.AddRange([Timer(6), Timer(2, kode: 2), Timer(1, fakturerbar: false)]);
        ao.KodeLinjer.Add(new() { Kode = "200", Navn = "Kjøring", Antall = 40, Kostpris = 4.5m, Salgspris = 9 });

        var linjer = ArbeidsordreOkonomiBeregner.BeregnLinjer(ao, Koder);

        Assert.Contains(linjer, l => l is { Varenummer: "100", Antall: 1, Belop: 5700 });
        Assert.Contains(linjer, l => l is { Varenummer: "110", Belop: 2850 });
        Assert.Contains(linjer, l => l is { Varenummer: "200", Belop: 360 });
        Assert.Equal(180m, ArbeidsordreOkonomiBeregner.AndreKost(ao.KodeLinjer));
    }

    [Fact]
    public void UtenTilbud_MedFastTimepris_BeholderMontasjelinjen()
    {
        var ao = new Arbeidsordre { Tittel = "AO", Timepris = 900, EstimerteTimer = 4 };
        ao.Timeregistreringer.Add(Timer(6));

        var linjer = ArbeidsordreOkonomiBeregner.BeregnLinjer(ao, Koder);

        Assert.Single(linjer);
        Assert.Equal(3600m, linjer[0].Belop);
    }

    [Fact]
    public void FaktiskAndre_BrukerDetSomErFort_PlussProsentAnslag()
    {
        // Budsjett 5 000: 4 000 kjøring/tillegg og 1 000 i prosentlinjer. Halvveis, 2 600 ført.
        var o = new ProsjektOkonomiService.Okonomi(1, ProsjektOkonomiService.OkonomiStatus.IRute, "", 100000,
            40, 22000, 30000, 20, 0, 11000, 15000, false, 0.5m, "", 40, 30000, 550, [], [],
            BudsjettAndre: 5000, BudsjettAndreProsent: 1000, FaktiskAndreRegistrert: 2600);

        Assert.True(o.AndreRegistrert);
        Assert.Equal(3100m, o.FaktiskAndre);
        Assert.Equal(6200m, o.PrognoseAndre);
    }

    [Fact]
    public void FaktiskAndre_UtenNoeFort_ErAnslag()
    {
        var o = new ProsjektOkonomiService.Okonomi(1, ProsjektOkonomiService.OkonomiStatus.IRute, "", 100000,
            40, 22000, 30000, 20, 0, 11000, 15000, false, 0.5m, "", 40, 30000, 550, [], [], BudsjettAndre: 5000);

        Assert.False(o.AndreRegistrert);
        Assert.Equal(2500m, o.FaktiskAndre);
        Assert.Equal(5000m, o.PrognoseAndre);
    }
}
