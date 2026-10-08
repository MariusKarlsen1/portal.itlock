using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Regresjonstester for lenke-skjema-filteret i PDF-forside-rendreren (se
// sikkerhetsgjennomgangen 2026-10-08, SECURITY_AUDIT.md funn 12).
public class ForsideRendererTests
{
    [Theory]
    [InlineData("http://itlock.no")]
    [InlineData("https://itlock.no/side")]
    [InlineData("HTTPS://ITLOCK.NO")]
    [InlineData("mailto:post@itlock.no")]
    public void ErTryggHyperlenke_TillatteSkjema_GirTrue(string href)
    {
        Assert.True(ForsideRenderer.ErTryggHyperlenke(href));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ErTryggHyperlenke_FarligeEllerManglendeSkjema_GirFalse(string? href)
    {
        Assert.False(ForsideRenderer.ErTryggHyperlenke(href));
    }
}
