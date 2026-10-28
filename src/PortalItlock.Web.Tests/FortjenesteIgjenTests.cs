using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// «Fortjeneste igjen» under Prosjektøkonomi: bare faktisk brukt mot tilbudt.
// En post som er brukt mindre enn tilbudt gir ikke mer fortjeneste ennå;
// en post som er brukt mer, trekker fra med det den er over.
public class FortjenesteIgjenTests
{
    // Tilbud: varer 55 760 i kost / 70 500 i salg, 22,1 t à 520 = 11 492 i kost / 22 434 i salg.
    private static ProsjektOkonomiService.Okonomi Lag(decimal faktiskTimer, decimal faktiskVarer, decimal jVarer = 0, decimal jTimer = 0, decimal jUforutsett = 0)
    {
        var tilbud = new List<ProsjektOkonomiService.TilbudInfo>
        {
            new(1, "Hovedtilbud", false, 92934, 55760, 22.1m, DateTime.Today, 0, 70500, 22434, 0, 11492),
        };
        return new ProsjektOkonomiService.Okonomi(1, ProsjektOkonomiService.OkonomiStatus.IRute, "", 92934,
            22.1m, 11492, 55760, faktiskTimer, 0, faktiskTimer * 520m, faktiskVarer, true,
            0.6m, "", faktiskTimer, faktiskVarer, 520, tilbud, [],
            jVarer + jTimer + jUforutsett, JusteringVarer: jVarer, JusteringTimer: jTimer, JusteringUforutsett: jUforutsett);
    }

    private static ProsjektOppfolgingService.Oppfolging Oppfolging(ProsjektOkonomiService.Okonomi o, decimal indeks = 0) =>
        new(1, o, 92934, 0, indeks, 0, 0, [], [], null, 0, null, false, 0, 0, [], [], "", ProsjektOppfolgingService.Niva.Noytral);

    [Fact]
    public void Innenfor_ErFortjenestenITilbudet()
    {
        var o = Lag(faktiskTimer: 10, faktiskVarer: 9121);

        Assert.Equal(92934 - 55760 - 11492, o.TilbudtFortjeneste);
        Assert.Equal(o.TilbudtFortjeneste, o.FortjenesteIgjen);
        Assert.False(o.TaperFortjeneste);
        Assert.Equal(ProsjektOppfolgingService.Niva.Gronn, Oppfolging(o).FortjenesteNiva);
    }

    [Fact]
    public void TimerOver_TrekkesFra()
    {
        var o = Lag(faktiskTimer: 58.5m, faktiskVarer: 9121);

        Assert.Equal(58.5m * 520 - 11492, o.OverTimer);
        Assert.Equal(0, o.OverVarer);
        Assert.Equal(o.TilbudtFortjeneste - o.OverTimer, o.FortjenesteIgjen);
        Assert.True(o.TaperFortjeneste);
        Assert.Equal(ProsjektOppfolgingService.Niva.Rod, Oppfolging(o).FortjenesteNiva);
        Assert.StartsWith("Timene er", Oppfolging(o).FortjenesteArsak);
    }

    [Fact]
    public void Justeringer_FlytterRammenOgReservenTrekkesFra()
    {
        // Varer 2 000 over tilbudet, men justert opp med 2 000: ingenting over.
        var o = Lag(faktiskTimer: 10, faktiskVarer: 57760, jVarer: 2000);
        Assert.Equal(0, o.OverVarer);
        Assert.Equal(o.TilbudtFortjeneste - 2000, o.FortjenesteIgjen);

        // Reserven til det uforutsette trekkes fra fortjenesten.
        var r = Lag(faktiskTimer: 10, faktiskVarer: 9121, jUforutsett: 3000);
        Assert.Equal(r.TilbudtFortjeneste - 3000, r.FortjenesteIgjen);
    }

    [Fact]
    public void Indeksregulering_LeggesTil()
    {
        var o = Lag(faktiskTimer: 10, faktiskVarer: 9121);
        Assert.Equal(o.FortjenesteIgjen + 1500, Oppfolging(o, indeks: 1500).FortjenesteIgjen);
    }

    [Fact]
    public void SalgPerPost_SummeresFraTilbudene()
    {
        var o = Lag(faktiskTimer: 10, faktiskVarer: 9121);
        Assert.Equal(70500, o.SalgVarer);
        Assert.Equal(22434, o.SalgArbeid);
        Assert.Equal(o.Inntekt, o.SalgVarer + o.SalgArbeid + o.SalgAndre);
    }
}
