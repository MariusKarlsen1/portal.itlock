using System.Text.Json;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Eldre koblingsskjema: rammen rundt komponenten krympes til bildet uten at
// bildet flytter seg, og streker som satt i kanten følger med.
public class KoblingsTilpasningTests
{
    private static KoblingsSymbol Kodeleser(double x, double y, double b, double h) => new()
    {
        ElementType = KoblingsElementType.Komponent,
        KomponentNokkel = "b-salto-kodeleser",
        PosX = x, PosY = y, Bredde = b, Hoyde = h
    };

    [Fact]
    public void ForHoyRamme_KrympesTilBildet_MedSammeMidtpunkt()
    {
        var k = KoblingsKomponentKatalog.Finn("b-salto-kodeleser")!;
        var s = Kodeleser(40, 20, 10, 30);
        Assert.NotNull(KoblingsTilpasning.TilpassTilBilde(s));
        Assert.Equal(10, s.Bredde, 3);
        Assert.Equal(10 * k.Forhold / KoblingsTilpasning.LerretAspekt, s.Hoyde, 2);
        Assert.Equal(20 + 15, s.PosY + s.Hoyde / 2, 2);
        Assert.Equal(45, s.PosX + s.Bredde / 2, 2);
    }

    [Fact]
    public void RiktigRamme_EndresIkke()
    {
        var k = KoblingsKomponentKatalog.Finn("b-salto-kodeleser")!;
        var s = Kodeleser(40, 20, 10, 10 * k.Forhold / KoblingsTilpasning.LerretAspekt);
        Assert.Null(KoblingsTilpasning.TilpassTilBilde(s));
    }

    [Fact]
    public void StrekIBunnenAvRammen_FlyttesInnTilNyKant()
    {
        var s = Kodeleser(40, 20, 10, 30);
        var strek = new KoblingsStrek
        {
            PunkterJson = JsonSerializer.Serialize(new List<KoblingsStrekPunkt> { new() { X = 45, Y = 50 }, new() { X = 45, Y = 70 } })
        };
        Assert.True(KoblingsTilpasning.TilpassSkjema([s], [strek]));
        var p = JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(strek.PunkterJson)!;
        Assert.Equal(s.PosY + s.Hoyde, p[0].Y, 2);
        Assert.Equal(45, p[0].X, 2);
        Assert.Equal(70, p[1].Y, 2);
    }
}
