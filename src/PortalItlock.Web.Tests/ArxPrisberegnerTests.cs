using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// ARX-kalkulatoren: sentral, dørsentraler, lisenser og batteri ut fra valgene.
public class ArxPrisberegnerTests
{
    private static int Antall(ArxResultat r, string kode) => r.Linjer.Where(l => l.Kode == kode).Sum(l => l.Antall);

    [Theory]
    [InlineData(1, ArxSentral.Ip9101)]
    [InlineData(2, ArxSentral.Power9008)]
    [InlineData(8, ArxSentral.Power9008)]
    [InlineData(9, ArxSentral.Voco9016)]
    [InlineData(24, ArxSentral.Voco9016)]
    public void AnbefaltSentral_FolgerAntallDorer(int dorer, ArxSentral forventet) =>
        Assert.Equal(forventet, ArxPrisberegner.Anbefalt(dorer));

    [Theory]
    [InlineData(1, new[] { 1 })]
    [InlineData(5, new[] { 4, 1 })]
    [InlineData(8, new[] { 8 })]
    [InlineData(12, new[] { 8, 4 })]
    [InlineData(80, new[] { 80 })]
    public void Dorlisenser_MinsteKombinasjon(int dorer, int[] pakker) =>
        Assert.Equal(pakker.OrderBy(x => x), ArxPrisberegner.Dorlisenskombinasjon(dorer).Select(k => k.Antall).OrderBy(x => x));

    [Fact]
    public void FemDorer_Gir9008MedDorkortDacOgToTjueAh()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 5 });

        Assert.Equal(ArxSentral.Power9008, r.Sentral);
        Assert.Equal(1, Antall(r, "S55299008V165"));
        Assert.Equal(1, Antall(r, "S5540144160"));
        Assert.Equal(5, Antall(r, "S55666300085"));
        Assert.Equal(5, Antall(r, "S559885J084"));
        Assert.Equal(2, Antall(r, "S511104084"));
        Assert.Equal(5, r.DekketeDorlisenser);
        // 225 + 5 × 35 + 5 × 50 = 650 mA i hvile -> 650 × 12 / 0,8 = 9,75 Ah
        Assert.Equal(650m, r.Enheter[0].HvileMa);
        Assert.Equal(9.75m, r.Enheter[0].BehovAh);
    }

    [Fact]
    public void EnDor_9101MedForsteDorRettPaSentralen()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 1, Timer = 1 });

        Assert.Equal(ArxSentral.Ip9101, r.Sentral);
        Assert.Equal(0, Antall(r, "S55666300085"));
        Assert.Equal(1, Antall(r, "S55299203165")); // 3PS holder for 1 time
    }

    [Fact]
    public void EnDor_TolvTimer_Gir5PsMedSyvKommaToAh()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 1, Timer = 12 });

        Assert.Equal(1, Antall(r, "S55299205165"));
        Assert.Equal(2, Antall(r, "S511107084"));
        Assert.Equal(0, Antall(r, "S55299203165"));
    }

    [Fact]
    public void PoE_GirIngenBatteribackup()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 1, Strom9101 = ArxStrom9101.PoE });

        Assert.DoesNotContain(r.Linjer, l => l.Gruppe == "Strøm");
        Assert.Contains(r.Advarsler, a => !a.Feil && a.Tekst.Contains("UPS"));
    }

    [Fact]
    public void Power9101S_BrukerAlltidTjueAh()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 1, Timer = 1, Sentral = ArxSentral.Power9101S });

        Assert.Equal(2, Antall(r, "S511104084"));
        Assert.Equal(0, Antall(r, "S511107084"));
    }

    [Fact]
    public void MagnetlaserOgLangDriftstid_Over40Ah_GirAdvarselPa9008()
    {
        var v = new ArxValg { Dorer = 8, Timer = 24 };
        v.Laser["slutt"] = 4;
        v.Laser["magnet"] = 4;
        var r = ArxPrisberegner.Beregn(v);

        Assert.True(r.Enheter[0].ForLite);
        Assert.Contains(r.Advarsler, a => a.Feil && a.Tekst.Contains("40"));
    }

    [Fact]
    public void Seksten_Magnetlaser_TrettiTimer_GirPscLMedFireBatteriskap()
    {
        var v = new ArxValg { Dorer = 16, Timer = 30 };
        v.Laser["slutt"] = 0;
        v.Laser["magnet"] = 16;
        var r = ArxPrisberegner.Beregn(v);

        Assert.Equal(1, Antall(r, "S55299310165"));
        Assert.Equal(4, Antall(r, "S55299300165"));
        Assert.Equal(10, Antall(r, "BATT45"));
        Assert.False(r.Enheter[0].ForLite);
    }

    [Fact]
    public void Alarm_GirSio64OgMinsteInngangspakke()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 5, Alarm = true, InngangerPerDor = 2, EkstraInnganger = 4 });

        Assert.Equal(5, Antall(r, "S5566504000"));
        Assert.Equal(0, Antall(r, "S55666300085"));
        Assert.Equal(1, Antall(r, "S55940020996")); // 14 innganger -> pakke på 20
    }

    [Fact]
    public void Utvidelse_TarIkkeMedGrunnpakke()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 3, Utvidelse = true });

        Assert.DoesNotContain(r.Linjer, l => l.Gruppe == "Programvare");
    }

    [Fact]
    public void ElkoBryter_GirImpulsfjaerPerDor()
    {
        var r = ArxPrisberegner.Beregn(new ArxValg { Dorer = 3, Knapp = "elko" });

        Assert.Equal(3, Antall(r, "ELKO-RS16"));
        Assert.Equal(3, Antall(r, ArxPrisberegner.Impulsfjaer));
    }
}
