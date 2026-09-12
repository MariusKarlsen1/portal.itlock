namespace PortalItlock.Web.Services;

// Grovt anslag på om forespørselen kommer fra en mobiltelefon, ut fra User-
// Agent-headeren. Brukes KUN som et førsteinntrykk under server-prerendering
// (før JS-interop kan bekrefte faktisk vindusbredde via matchMedia), slik at
// den statisk rendrede HTML-en som vises aller først på ekte telefoner
// allerede er riktig - i stedet for at skrivebordsmarkup blafrer synlig et
// øyeblikk før den interaktive kretsen retter det opp. Den endelige sannheten
// er fortsatt vindusbredden (window.erMobilvisning() i wwwroot/js/sidebar.js),
// ikke denne.
public static class MobilDeteksjon
{
    public static bool GjettFraUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return false;
        }

        return userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase);
    }
}
