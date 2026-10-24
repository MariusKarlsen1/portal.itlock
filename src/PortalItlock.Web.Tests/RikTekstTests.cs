using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Formatert tekst i sluttrapporten: rensing, lister og ren tekst.
public class RikTekstTests
{
    [Fact]
    public void RenTekst_BlirAvsnitt()
    {
        Assert.Equal("<p>Linje 1</p><p>Linje 2 &amp; mer</p>", RikTekst.TilHtml("Linje 1\nLinje 2 & mer"));
    }

    [Fact]
    public void Rens_FjernerUkjenteTaggerOgAttributter()
    {
        var html = "<div style=\"color:red\"><script>alert(1)</script><b onclick=\"x()\">Fet</b> og <span>vanlig</span></div><img src=x onerror=y>";

        var renset = RikTekst.Rens(html);

        Assert.Equal("<p>alert(1)<b>Fet</b> og vanlig</p>", renset);
        Assert.DoesNotContain("onclick", renset);
        Assert.DoesNotContain("<img", renset);
    }

    [Fact]
    public void Lister_OgStorrelserBeholdes()
    {
        var html = "<h3>Overskrift</h3><h4>Stor</h4><ul><li>En</li><li><i>To</i></li></ul><ol><li>Første</li><li>Andre</li></ol>";

        Assert.Equal(html, RikTekst.TilHtml(html));
        Assert.Equal("Overskrift\nStor\n• En\n• To\n1. Første\n2. Andre", RikTekst.TilTekst(html));
    }

    [Fact]
    public void TomtFelt_LagresSomNull()
    {
        Assert.Null(RikTekst.Rens("<p><br></p>"));
        Assert.Null(RikTekst.Rens("   "));
    }
}
