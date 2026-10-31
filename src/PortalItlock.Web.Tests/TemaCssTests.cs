using PortalItlock.Web.Models;
using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

public class TemaCssTests
{
    [Fact]
    public void Omfarg_FlytterBruntTilBlaatOgBeholderStatusOgGraa()
    {
        var css = Omfarg(".a{color:#835e41;background:rgba(131, 94, 65, .12)}.w{color:#c98a1e}.g{color:#777777}.r{color:#c0392b}");

        Assert.DoesNotContain("#835e41", css);
        Assert.DoesNotContain("131, 94, 65", css);
        Assert.Contains(", .12)", css);
        Assert.Contains("#c98a1e", css);
        Assert.Contains("#777777", css);
        Assert.Contains("#c0392b", css);
    }

    [Fact]
    public void Flytt_GirBlaaFargeMedOmtrentSammeLyshet()
    {
        var (r, g, b) = TemaCss.Flytt(0x83, 0x5e, 0x41, 214)!.Value;

        Assert.True(b > r && b > g);
        Assert.InRange((r + g + b) / 3, 60, 120);
    }

    [Fact]
    public void Href_BrukerOmfargetFilBareForTemaMedOmfarging()
    {
        Assert.Equal("tema/Marinbla/app.css?v=3", TemaCss.Href(OrganisasjonTema.Marinbla, "app.css", 3));
        Assert.Equal("app.css?v=3", TemaCss.Href(OrganisasjonTema.Standard, "app.css", 3));
        Assert.Equal("app.css?v=3", TemaCss.Href(null, "app.css", 3));
        Assert.Equal("kassa.css?v=3", TemaCss.Href(OrganisasjonTema.Marinbla, "kassa.css", 3));
    }

    private static string Omfarg(string css) => TemaCss.Omfarg(css, 214);
}
