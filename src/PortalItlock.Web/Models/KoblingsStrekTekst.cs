namespace PortalItlock.Web.Models;

// Hvor teksten ved en strekende skal stå: rett over streken når den går
// vannrett, til høyre for den når den går loddrett, og alltid inn mot streken
// (ikke ut over komponenten den er koblet til).
public static class KoblingsStrekTekst
{
    public sealed record Plassering(double X, double Y, string Klasse, string Tekst);

    public static IEnumerable<Plassering> For(KoblingsStrek s, List<KoblingsStrekPunkt> p, double aspekt)
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
