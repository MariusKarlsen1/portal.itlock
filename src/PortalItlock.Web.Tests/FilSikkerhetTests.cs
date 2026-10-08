namespace PortalItlock.Web.Tests;

// Regresjonstester for FilSikkerhet.TryggContentType (definert i
// Program.cs, global namespace, synlig her via InternalsVisibleTo) -
// forsvaret mot lagret XSS via filopplasting (se sikkerhetsgjennomgangen
// 2026-10-08, SECURITY_AUDIT.md funn 1).
public class FilSikkerhetTests
{
    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    [InlineData("image/bmp")]
    [InlineData("text/csv")]
    [InlineData("text/plain")]
    [InlineData("application/octet-stream")]
    public void TryggContentType_KjenteTryggeTyper_SlippesGjennomUendret(string tryggType)
    {
        Assert.Equal(tryggType, FilSikkerhet.TryggContentType(tryggType));
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("application/javascript")]
    [InlineData("text/javascript")]
    [InlineData("application/xhtml+xml")]
    [InlineData("image/svg+xml")]
    [InlineData("application/x-msdownload")]
    public void TryggContentType_FarligeTyper_NedgraderesTilOctetStream(string farligType)
    {
        // Dette er selve forsvaret: en opplastet fil med Content-Type
        // "text/html" (eller lignende) skal ALDRI serveres tilbake som
        // det den utgir seg for å være - bare som en generisk nedlasting.
        Assert.Equal("application/octet-stream", FilSikkerhet.TryggContentType(farligType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryggContentType_TomEllerManglende_GirOctetStream(string? ingenType)
    {
        Assert.Equal("application/octet-stream", FilSikkerhet.TryggContentType(ingenType));
    }

    [Fact]
    public void TryggContentType_ErIkkeCaseSensitiv()
    {
        Assert.Equal("IMAGE/PNG", FilSikkerhet.TryggContentType("IMAGE/PNG"));
        Assert.Equal("application/octet-stream", FilSikkerhet.TryggContentType("TEXT/HTML"));
    }

    [Fact]
    public void TryggContentType_IgnorererParameterEtterSemikolon()
    {
        // Nettlesere kan sende f.eks. "text/html; charset=utf-8" -
        // charset-delen skal ikke hindre gjenkjenning av selve typen.
        Assert.Equal("application/octet-stream", FilSikkerhet.TryggContentType("text/html; charset=utf-8"));
        Assert.Equal("image/png", FilSikkerhet.TryggContentType("image/png; charset=binary"));
    }
}
