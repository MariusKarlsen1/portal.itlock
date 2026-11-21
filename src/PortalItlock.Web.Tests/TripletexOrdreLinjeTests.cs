using PortalItlock.Web.Models;
using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

// Ordrelinjene som sendes til Tripletex ved ferdigmelding: «produktkode navn»,
// veiledende pris med rabatt, og innkjøpspris - som når ordren legges inn for hånd.
public class TripletexOrdreLinjeTests
{
    private static ArbeidsordreOkonomiBeregner.OkonomiLinje Linje(decimal antall, decimal utpris, decimal? liste, decimal? inn = null, string? kode = null, string navn = "Ovalsylinder S50") =>
        new(navn, null, null, null, antall, antall * utpris, Innpris: inn, Listepris: liste, Produktkode: kode);

    [Fact]
    public void Rabatt_fra_veiledende_pris_gir_samme_belop()
    {
        var t = ArbeidsordreOkonomiBeregner.TripletexLinje(Linje(10, 4068.10m, 4786m, 3350.20m, "C50S.111.SD.SE"));

        Assert.Equal("C50S.111.SD.SE Ovalsylinder S50", t.Beskrivelse);
        Assert.Equal(4786m, t.Enhetspris);
        Assert.Equal(15m, t.RabattProsent);
        Assert.Equal(3350.20m, t.Innpris);
    }

    [Fact]
    public void Skjev_rabatt_avviker_under_en_ore_per_stk()
    {
        var t = ArbeidsordreOkonomiBeregner.TripletexLinje(Linje(3, 1234.57m, 1999m));
        var pris = t.Enhetspris * (1 - t.RabattProsent / 100m);
        Assert.True(Math.Abs(pris - 1234.57m) < 0.01m);
    }

    [Fact]
    public void Uten_veiledende_pris_gaar_salgsprisen_rett_inn()
    {
        var t = ArbeidsordreOkonomiBeregner.TripletexLinje(Linje(2, 500m, null, 300m));
        Assert.Equal(500m, t.Enhetspris);
        Assert.Equal(0m, t.RabattProsent);
    }

    [Fact]
    public void Over_veiledende_pris_gir_ingen_rabatt()
    {
        var t = ArbeidsordreOkonomiBeregner.TripletexLinje(Linje(1, 1200m, 1000m));
        Assert.Equal(1200m, t.Enhetspris);
        Assert.Equal(0m, t.RabattProsent);
    }

    [Fact]
    public void Produktkoden_dobles_ikke_naar_navnet_allerede_har_den()
    {
        var t = ArbeidsordreOkonomiBeregner.TripletexLinje(Linje(1, 99m, null, kode: "E5.N18.15", navn: "E5.N18.15 Rund sylinderring 18mm"));
        Assert.Equal("E5.N18.15 Rund sylinderring 18mm", t.Beskrivelse);
    }

    [Fact]
    public void Til_kontroll_bare_til_kontoret_har_ferdigmeldt()
    {
        var a = new Arbeidsordre { Tittel = "x", MontorFerdigmeldtDato = DateTime.Now, Status = ArbeidsordreStatus.Pagaende };
        Assert.True(a.ErTilKontroll);
        a.Status = ArbeidsordreStatus.Ferdig;
        Assert.False(a.ErTilKontroll);
    }
}

public class TripletexEpostTests
{
    [Theory]
    [InlineData("post@itlock.no", "post@itlock.no")]
    [InlineData(" post@itlock.no ", "post@itlock.no")]
    [InlineData("post@itlock", null)]
    [InlineData("ola@a.no, kari@b.no", null)]
    [InlineData("ola nordmann@a.no", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Ugyldig_epost_sendes_ikke(string? inn, string? ut) =>
        Assert.Equal(ut, TripletexService.GyldigEpost(inn));
}
