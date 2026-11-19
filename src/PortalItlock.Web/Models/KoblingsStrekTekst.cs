namespace PortalItlock.Web.Models;

// Hvor teksten ved en strekende skal stå: rett over streken når den går
// vannrett, til høyre for den når den går loddrett, og alltid inn mot streken
// (ikke ut over komponenten den er koblet til).
public static class KoblingsStrekTekst
{
    // Hvit (eller nesten hvit) strek: tegnes med grå kant, og merkingen blir sort
    // med hvit boks rundt, så begge synes mot hvit bakgrunn.
    public const string HvitKant = "#9a948c";

    public static bool ErHvit(string? farge)
    {
        if (string.IsNullOrWhiteSpace(farge) || !farge.StartsWith('#'))
        {
            return false;
        }
        var h = farge.TrimStart('#');
        if (h.Length == 3)
        {
            h = string.Concat(h.Select(c => $"{c}{c}"));
        }
        if (h.Length < 6 || !int.TryParse(h[..6], System.Globalization.NumberStyles.HexNumber, null, out var v))
        {
            return false;
        }
        return ((v >> 16) & 255) >= 225 && ((v >> 8) & 255) >= 225 && (v & 255) >= 225;
    }

    // Fargen på merkingen: valgt farge, ellers strekens farge - men sort på hvite streker.
    public static string Farge(KoblingsStrek s) => s.TekstFarge ?? (ErHvit(s.Farge) ? "#14213D" : s.Farge);

    public sealed record Plassering(double X, double Y, string Klasse, string Tekst);

    // Tekst ved enden av hver leder i en flerlederkabel. Plasseringen («over» eller
    // «på enden») følger valget på kabelen. Farge = lederens farge, sort på hvit leder.
    public sealed record LederPlassering(Plassering P, string Farge, bool Hvit);

    public static IEnumerable<LederPlassering> ForLedere(KoblingsStrek s, KoblingsKabel.Vifte? v, double aspekt)
    {
        if (v is null)
        {
            yield break;
        }
        foreach (var l in v.Ledere)
        {
            var tekst = l.ErStart ? l.Leder.TekstStart : l.Leder.TekstSlutt;
            if (string.IsNullOrWhiteSpace(tekst) || l.Punkter.Count < 2 || Ved(l.Punkter[^1], l.Punkter[^2], aspekt) is not { } p)
            {
                continue;
            }
            var hvit = l.Leder.Farge == "Hvit";
            var plass = p with { Tekst = tekst.Trim() };
            if (s.TekstVedEnden)
            {
                plass = plass with { Klasse = plass.Klasse.Replace("ks-st-", "ks-se-") };
            }
            yield return new LederPlassering(plass, s.TekstFarge ?? (hvit ? "#14213D" : KoblingsKabel.Hex(l.Leder.Farge)), hvit);
        }
    }

    public static bool HarLederTekst(KoblingsStrek s) =>
        s.ErKabel && !string.IsNullOrEmpty(s.LedereJson) && s.LedereJson.Contains("\"Tekst", StringComparison.Ordinal)
        && KoblingsKabel.Les(s.LedereJson).Any(l => !string.IsNullOrWhiteSpace(l.TekstStart) || !string.IsNullOrWhiteSpace(l.TekstSlutt));

    public static IEnumerable<Plassering> For(KoblingsStrek s, List<KoblingsStrekPunkt> p, double aspekt)
    {
        foreach (var t in Plasser(s, p, aspekt))
        {
            // På enden: teksten står i forlengelsen av streken, midt på linja.
            yield return s.TekstVedEnden ? t with { Klasse = t.Klasse.Replace("ks-st-", "ks-se-") } : t;
        }
    }

    private static IEnumerable<Plassering> Plasser(KoblingsStrek s, List<KoblingsStrekPunkt> p, double aspekt)
    {
        if (p.Count < 2)
        {
            yield break;
        }
        if (!string.IsNullOrWhiteSpace(s.TekstStart) && Ved(p[0], p[1], aspekt) is { } a)
        {
            yield return a with { Tekst = s.TekstStart.Trim() };
        }
        if (!string.IsNullOrWhiteSpace(s.TekstSlutt) && Ved(p[^1], p[^2], aspekt) is { } b)
        {
            yield return b with { Tekst = s.TekstSlutt.Trim() };
        }
    }

    private static Plassering? Ved(KoblingsStrekPunkt ende, KoblingsStrekPunkt neste, double aspekt)
    {
        double dx = neste.X - ende.X, dy = (neste.Y - ende.Y) * aspekt;
        if (Math.Abs(dx) < 1e-6 && Math.Abs(dy) < 1e-6)
        {
            return null;
        }
        var klasse = Math.Abs(dx) >= Math.Abs(dy)
            ? (dx > 0 ? "ks-st-hoyre" : "ks-st-venstre")
            : (dy > 0 ? "ks-st-ned" : "ks-st-opp");
        return new Plassering(ende.X, ende.Y, klasse, "");
    }
}
