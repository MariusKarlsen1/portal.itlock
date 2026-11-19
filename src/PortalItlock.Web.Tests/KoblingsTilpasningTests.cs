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

public class KoblingsStrekTekstTests
{
    private static List<KoblingsStrekPunkt> P(params double[] xy) =>
        Enumerable.Range(0, xy.Length / 2).Select(i => new KoblingsStrekPunkt { X = xy[2 * i], Y = xy[2 * i + 1] }).ToList();

    [Fact]
    public void TekstVedEndene_StarInnMotStreken()
    {
        var s = new KoblingsStrek { PunkterJson = "[]", TekstStart = "+12V", TekstSlutt = "NO" };
        var t = KoblingsStrekTekst.For(s, P(10, 50, 40, 50, 40, 80), 13.0 / 16.0).ToList();
        Assert.Equal(2, t.Count);
        Assert.Equal(("+12V", "ks-st-hoyre", 10.0), (t[0].Tekst, t[0].Klasse, t[0].X));
        Assert.Equal(("NO", "ks-st-opp", 80.0), (t[1].Tekst, t[1].Klasse, t[1].Y));
    }

    [Fact]
    public void TomTekst_GirIngenting()
    {
        var s = new KoblingsStrek { PunkterJson = "[]", TekstStart = "  " };
        Assert.Empty(KoblingsStrekTekst.For(s, P(10, 50, 40, 50), 1));
    }

    [Fact]
    public void TekstVedEnden_GirEgneKlasser()
    {
        var s = new KoblingsStrek { PunkterJson = "[]", TekstStart = "NO", TekstVedEnden = true };
        var t = Assert.Single(KoblingsStrekTekst.For(s, P(10, 50, 40, 50), 1));
        Assert.Equal("ks-se-hoyre", t.Klasse);
    }
}
