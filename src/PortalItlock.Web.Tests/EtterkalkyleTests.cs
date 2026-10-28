using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// «Slik gikk det» i etterkalkylen: fra kalkulert resultat ved kontrakt til
// faktisk resultat skal gå opp, og dommen skal følge dekningsgraden.
public class EtterkalkyleTests
{
    private static ProsjektOppfolgingService.Etterkalkyle Lag(decimal faktiskVarer, decimal faktiskTimer, bool fraLager = true, decimal indeks = 4200)
    {
        // Kontrakt 842 000 og én godkjent endring på 51 000 (20 000 i varer og 22 t).
        var tilbud = new List<ProsjektOkonomiService.TilbudInfo>
        {
            new(1, "Hovedtilbud", false, 842000, 376000, 305, DateTime.Today),
            new(2, "E1", true, 51000, 20000, 22, DateTime.Today),
        };
        var o = new ProsjektOkonomiService.Okonomi(1, ProsjektOkonomiService.OkonomiStatus.IRute, "", 893000,
            327, 327 * 550m, 396000, faktiskTimer, 0, faktiskTimer * 550m, faktiskVarer, fraLager,
            1, "", faktiskTimer, faktiskVarer, 550, tilbud, []);
        return new ProsjektOppfolgingService.Etterkalkyle(1, o, 842000, 51000, indeks, 20000 + 22 * 550m, 0,
            new DateTime(2026, 12, 15), new DateTime(2026, 12, 18), 0, 0, []);
    }

    [Fact]
    public void Bidragene_GarOppIFaktiskResultat()
    {
        var ek = Lag(faktiskVarer: 403500, faktiskTimer: 344);

        var bidrag = ek.Bidrag;
        var start = bidrag.First();
        var slutt = bidrag.Last();

        Assert.True(start.Total && slutt.Total);
        Assert.Equal(slutt.Belop, start.Belop + bidrag.Where(b => !b.Total).Sum(b => b.Belop));
        Assert.Equal(ek.FaktiskResultat, slutt.Belop);
    }

    [Fact]
    public void Dom_FolgerDekningsgraden()
    {
        Assert.Equal(ProsjektOppfolgingService.Dom.Som, Lag(faktiskVarer: 396000, faktiskTimer: 330).Dom);
        Assert.Equal(ProsjektOppfolgingService.Dom.Darligere, Lag(faktiskVarer: 430000, faktiskTimer: 400).Dom);
        Assert.Equal(ProsjektOppfolgingService.Dom.Bedre, Lag(faktiskVarer: 370000, faktiskTimer: 300).Dom);
    }

    [Fact]
    public void ForeslattArsak_ErDetStorsteAvviket()
    {
        Assert.Equal(EtterkalkyleArsak.Varekostnad, Lag(faktiskVarer: 440000, faktiskTimer: 330).ForeslattArsak);
        // Timer over, og prosjektet hadde endringer: ekstraarbeid.
        Assert.Equal(EtterkalkyleArsak.Ekstraarbeid, Lag(faktiskVarer: 396000, faktiskTimer: 420).ForeslattArsak);
    }

    [Fact]
    public void ManglerData_NarTimerEllerVarerIkkeErRegistrert()
    {
        // Timer og varer registrert: kan vurderes. At ikke alt er fakturert, er bare til opplysning.
        var ok = Lag(faktiskVarer: 396000, faktiskTimer: 330);
        Assert.False(ok.ManglerData);
        Assert.Contains(ok.Mangler, m => m.Kode == "faktura");

        Assert.True(Lag(faktiskVarer: 396000, faktiskTimer: 0).ManglerData);
        Assert.True(Lag(faktiskVarer: 396000, faktiskTimer: 330, fraLager: false).ManglerData);
    }

    [Fact]
    public void VarerAnslatt_NarVarerIkkeErRegistrert()
    {
        Assert.True(Lag(faktiskVarer: 396000, faktiskTimer: 330, fraLager: false).VarerAnslatt);
        Assert.False(Lag(faktiskVarer: 396000, faktiskTimer: 330).VarerAnslatt);
    }

    [Fact]
    public void KjoringOgTillegg_ErEgenPostOgGarOpp()
    {
        // 16 000 i kjøring og småmateriell fra kodelinjene, halvveis utført.
        var tilbud = new List<ProsjektOkonomiService.TilbudInfo> { new(1, "Hovedtilbud", false, 100000, 30000, 40, DateTime.Today, 16000) };
        var o = new ProsjektOkonomiService.Okonomi(1, ProsjektOkonomiService.OkonomiStatus.IRute, "", 100000,
            40, 40 * 550m, 30000, 20, 0, 20 * 550m, 15000, false, 0.5m, "", 40, 30000, 550, tilbud, [], BudsjettAndre: 16000);
        var ek = new ProsjektOppfolgingService.Etterkalkyle(1, o, 100000, 0, 0, 0, 0, null, null, 0, 0, []);

        Assert.Equal(8000m, o.FaktiskAndre);
        Assert.Equal(30000m + 22000m + 16000m, o.BudsjettKost);
        Assert.Equal(ek.KalkVarer + ek.KalkLonn + 16000m, ek.KalkKost);
        Assert.Equal(ek.FaktiskResultat, ek.Bidrag.First().Belop + ek.Bidrag.Where(b => !b.Total).Sum(b => b.Belop));
    }

    [Fact]
    public void DagerOverFrist_RegnesFraOverlevertDato()
    {
        Assert.Equal(3, Lag(faktiskVarer: 396000, faktiskTimer: 330).DagerOverFrist);
    }
}
