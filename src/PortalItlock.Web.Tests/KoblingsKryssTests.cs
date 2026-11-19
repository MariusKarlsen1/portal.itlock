using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Koblingsskjema: halvmåne der streker krysser, prikk der en strek ender på en annen.
public class KoblingsKryssTests
{
    private static KoblingsKryss.Linje L(params double[] xy) =>
        new(Enumerable.Range(0, xy.Length / 2).Select(i => new KoblingsStrekPunkt { X = xy[2 * i], Y = xy[2 * i + 1] }).ToList(), "#000", 2);

    [Fact]
    public void Kryss_GirHalvmane_IngenPrikk()
    {
        var (hopp, prikker) = KoblingsKryss.Finn([L(10, 50, 90, 50), L(50, 10, 50, 90)], 1);
        Assert.Single(hopp);
        Assert.Empty(prikker);
        Assert.Equal(1, hopp[0].Over);
    }

    [Fact]
    public void StrekSomEnderPaAnnen_GirPrikk_IngenHalvmane()
    {
        var (hopp, prikker) = KoblingsKryss.Finn([L(10, 50, 90, 50), L(50, 10, 50, 50)], 1);
        Assert.Empty(hopp);
        var p = Assert.Single(prikker);
        Assert.Equal(50, p.Punkt.X, 3);
        Assert.Equal(50, p.Punkt.Y, 3);
    }

    [Fact]
    public void ToStrekerEndeMotEnde_GirIngenPrikk()
    {
        var (hopp, prikker) = KoblingsKryss.Finn([L(10, 50, 50, 50), L(50, 50, 50, 90)], 1);
        Assert.Empty(hopp);
        Assert.Empty(prikker);
        Assert.NotEmpty(KoblingsKryss.Svg([L(10, 50, 90, 50), L(50, 10, 50, 90)], 0.8));
    }

    [Fact]
    public void ToTetteLedere_GirEnBue_IkkeTo()
    {
        // Rød loddrett strek over grønn og gul som går tett side om side.
        var linjer = new List<KoblingsKryss.Linje> { L(10, 50, 90, 50), L(10, 50.6, 90, 50.6), L(50, 10, 50, 90) };
        var (hopp, _) = KoblingsKryss.Finn(linjer, 1);
        Assert.Equal(2, hopp.Count);
        var buer = KoblingsKryss.SlaSammen(hopp, 1);
        var bue = Assert.Single(buer);
        Assert.True(bue.B.Y - bue.A.Y < 2, "Buen skal være liten");
    }

    [Fact]
    public void KryssLangtFraHverandre_GirToBuer()
    {
        var linjer = new List<KoblingsKryss.Linje> { L(10, 30, 90, 30), L(10, 70, 90, 70), L(50, 10, 50, 90) };
        var (hopp, _) = KoblingsKryss.Finn(linjer, 1);
        Assert.Equal(2, KoblingsKryss.SlaSammen(hopp, 1).Count);
    }
}

public class KoblingsKabelLengdeTests
{
    private static List<KoblingsStrekPunkt> P(params double[] xy) =>
        Enumerable.Range(0, xy.Length / 2).Select(i => new KoblingsStrekPunkt { X = xy[2 * i], Y = xy[2 * i + 1] }).ToList();

    [Fact]
    public void LederMedEgenLengde_StikkerLengreUt()
    {
        var ledere = new List<KoblingsLeder> { new() { Farge = "Rød" }, new() { Farge = "Blå", LengdeStart = 2.0 } };
        var v = KoblingsKabel.LagVifte(P(50, 50, 90, 50), ledere, 1)!;
        var start = v.Ledere.Where(l => l.ErStart).ToDictionary(l => l.Indeks, l => l.Punkter[^1].X);
        // Kabelen går mot høyre, så lederne stikker ut mot venstre: lengre = lavere X.
        Assert.True(start[1] < start[0] - 1);
    }

    [Fact]
    public void KabelnavnVedStart_LiggerNaerStart()
    {
        var ledere = new List<KoblingsLeder> { new() };
        var midt = KoblingsKabel.LagVifte(P(10, 50, 90, 50), ledere, 1)!.Merke;
        var start = KoblingsKabel.LagVifte(P(10, 50, 90, 50), ledere, 1, merkePlass: "start")!.Merke;
        Assert.Equal(50, midt.X, 1);
        Assert.True(start.X < 20);
    }
}
