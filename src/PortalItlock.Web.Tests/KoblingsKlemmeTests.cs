using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Klemmerekker i koblingsskjemaet: ett koblingspunkt per skrue, midt på hver pol.
public class KoblingsKlemmeTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void Klemmerekke_HarEttPunktPerSkrue(int poler)
    {
        var k = KoblingsKomponentKatalog.Finn($"k-klemme-{poler}");
        Assert.NotNull(k);
        var porter = KoblingsPorter.Les(k!.Porter);
        Assert.Equal(poler, porter.Count);
        Assert.All(porter, p => Assert.Equal(0.5, p.X));
        Assert.True(porter.Zip(porter.Skip(1)).All(par => par.Second.Y > par.First.Y));
        Assert.True(k.UtenNavn);
    }

    [Fact]
    public void SvgTekst_EscaperInnhold()
    {
        var svg = KoblingsSvg.Tekst(10, 20, 1.5, "#14213D", "<K1 & K2>", midt: false);
        Assert.Contains("&lt;K1 &amp; K2&gt;", svg);
        Assert.StartsWith("<text ", svg);
    }
}
