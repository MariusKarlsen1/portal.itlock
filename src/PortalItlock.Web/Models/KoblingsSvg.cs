using System.Globalization;
using System.Net;

namespace PortalItlock.Web.Models;

// Små SVG-biter til koblingsskjemaene. Tekst-taggen kan ikke skrives rett i
// Razor (reservert ord), så den bygges her.
public static class KoblingsSvg
{
    private static string Pos(double v) => v.ToString(CultureInfo.InvariantCulture);

    public static string Tekst(double x, double y, double storrelse, string farge, string tekst, bool midt, string anker = "middle") =>
        $"<text x=\"{Pos(x)}\" y=\"{Pos(y)}\" text-anchor=\"{anker}\"{(midt ? " dominant-baseline=\"middle\"" : "")} " +
        $"font-size=\"{Pos(storrelse)}\" font-weight=\"700\" fill=\"{WebUtility.HtmlEncode(farge)}\" stroke=\"#fff\" " +
        $"stroke-width=\"{Pos(storrelse / 3)}\" paint-order=\"stroke\">{WebUtility.HtmlEncode(tekst)}</text>";
}
