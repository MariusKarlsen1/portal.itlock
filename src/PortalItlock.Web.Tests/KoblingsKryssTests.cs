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
}
