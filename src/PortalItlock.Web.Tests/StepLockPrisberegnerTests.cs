using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Tallene er lest av forhandlerarket "StepLock Access Priskonfigurator"
// (versjon 3. sept 2025) ved å skrive inn antall og tilkobling.
public class StepLockPrisberegnerTests
{
    private static readonly Dictionary<string, decimal> Netto = new()
    {
        ["ST2029-POE"] = 3684, ["ST2020-S"] = 13643, ["ST2022"] = 2729, ["ST2010"] = 2422, ["ST2011"] = 3354,
        ["ST2066-MF"] = 33, ["ST2042-POE"] = 2581, ["ST2042-DCE"] = 3230, ["ST241"] = 2418, ["ST2063"] = 294,
        ["ST2071"] = 513, ["BASIC"] = 264, ["STANDARD"] = 569, ["PREMIUM"] = 1028, ["M-001"] = 196, ["M-002"] = 78
    };

    private static StepLockResultat Regn(StepLockValg v) => StepLockPrisberegner.Beregn(v, k => Netto[k]);

    [Theory]
    [InlineData(0, 0, StepLockAbonnement.Basic)]
    [InlineData(1, 0, StepLockAbonnement.Basic)]
    [InlineData(2, 0, StepLockAbonnement.Standard)]
    [InlineData(10, 0, StepLockAbonnement.Standard)]
    [InlineData(11, 0, StepLockAbonnement.Premium)]
    [InlineData(20, 0, StepLockAbonnement.Premium)]
    [InlineData(21, 0, StepLockAbonnement.Basic)]
    [InlineData(100, 0, StepLockAbonnement.Basic)]
    [InlineData(0, 4, StepLockAbonnement.Basic)]
    [InlineData(0, 6, StepLockAbonnement.Standard)]
    [InlineData(0, 15, StepLockAbonnement.Standard)]
    [InlineData(0, 16, StepLockAbonnement.Premium)]
    [InlineData(0, 300, StepLockAbonnement.Premium)]
    [InlineData(1, 4, StepLockAbonnement.Basic)]
    [InlineData(1, 5, StepLockAbonnement.Basic)]
    [InlineData(1, 6, StepLockAbonnement.Standard)]
    [InlineData(2, 4, StepLockAbonnement.Standard)]
    [InlineData(2, 16, StepLockAbonnement.Premium)]
    [InlineData(10, 15, StepLockAbonnement.Standard)]
    [InlineData(11, 4, StepLockAbonnement.Premium)]
    [InlineData(11, 6, StepLockAbonnement.Premium)]
    [InlineData(20, 6, StepLockAbonnement.Premium)]
    [InlineData(21, 4, StepLockAbonnement.Basic)]
    [InlineData(21, 16, StepLockAbonnement.Premium)]
    public void Abonnement_velges_som_i_arket(int porttelefoner, int kortlesere, StepLockAbonnement ventet) =>
        Assert.Equal(ventet, StepLockPrisberegner.VelgAbonnement(porttelefoner, kortlesere));

    [Theory]
    [InlineData(0, 5)]
    [InlineData(21, 5)]
    [InlineData(21, 6)]
    [InlineData(21, 15)]
    public void Hull_i_arket_gir_ikke_abonnement(int porttelefoner, int kortlesere) =>
        Assert.Null(StepLockPrisberegner.VelgAbonnement(porttelefoner, kortlesere));

    [Fact]
    public void Porttelefon_og_kortlesere_gir_samme_sum_som_arket()
    {
        var r = Regn(new StepLockValg(1, 0, 6, 0, 0));
        Assert.Equal(48152, r.Hardware);
        Assert.Equal(569, r.PerManed);
        Assert.Equal(54980, r.ForsteAr);
    }

    [Fact]
    public void Porttelefon_med_4G_far_router_og_tillegg()
    {
        var r = Regn(new StepLockValg(2, 0, 0, 0, 0, StepLockTilkobling.Lte));
        Assert.Equal(41104, r.Hardware);
        Assert.Equal(392, r.Tillegg4G);
    }

    [Fact]
    public void Alt_pa_4G_gir_samme_sum_som_arket()
    {
        var r = Regn(new StepLockValg(1, 0, 2, 3, 0, StepLockTilkobling.Lte, StepLockTilkobling.Lte, StepLockTilkobling.Lte));
        Assert.Equal(60453, r.Hardware);
        Assert.Equal(586, r.Tillegg4G);
        Assert.Equal(StepLockAbonnement.Basic, r.Abonnement);
    }

    // Spesifikasjonen i arket er fasit. Sammendraget der blander prisene ved DCE.
    [Fact]
    public void DCE_gir_samme_sum_som_spesifikasjonen_i_arket()
    {
        var r = Regn(new StepLockValg(1, 0, 2, 0, 0, StepLockTilkobling.Lte, StepLockTilkobling.Dce));
        Assert.Equal(31856, r.Hardware);
        Assert.Equal(196, r.Tillegg4G);
    }

    [Fact]
    public void Blandet_DCE_og_4G_gir_samme_sum_som_spesifikasjonen_i_arket()
    {
        var r = Regn(new StepLockValg(0, 0, 2, 3, 0, StepLockTilkobling.PoE, StepLockTilkobling.Lte, StepLockTilkobling.Dce));
        Assert.Equal(34594, r.Hardware);
        Assert.Equal(156, r.Tillegg4G);
        Assert.Null(r.Abonnement);
    }

    [Fact]
    public void Svarapparater_og_brikker_endrer_ikke_abonnementet()
    {
        Assert.Equal(StepLockAbonnement.Basic, Regn(new StepLockValg(0, 21, 0, 0, 500)).Abonnement);
        Assert.Equal(57309 + 16500, Regn(new StepLockValg(0, 21, 0, 0, 500)).Hardware);
    }

    [Fact]
    public void Pris_per_enhet_er_med_alt_som_horer_til()
    {
        Assert.Equal(18134, StepLockPrisberegner.PrisPorttelefon(StepLockTilkobling.PoE, k => Netto[k]));
        Assert.Equal(5003, StepLockPrisberegner.PrisKortleser("ST2010", StepLockTilkobling.PoE, k => Netto[k]));
        Assert.Equal(5935, StepLockPrisberegner.PrisKortleser("ST2011", StepLockTilkobling.PoE, k => Netto[k]));
    }
}
