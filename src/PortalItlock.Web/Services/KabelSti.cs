using System.Globalization;

namespace PortalItlock.Web.Services;

// Kabeltrekk på plantegningen: knekkpunktene lagres som "x,y;x,y" (prosent av
// tegningen), og kabelen tegnes som en myk kurve gjennom endepunktene og
// knekkpunktene (Catmull-Rom omgjort til kubiske Bézier-kurver). Samme
// utregning brukes på skjermen (plantegning.js) og i PDF-en.
public static class KabelSti
{
    public static List<(double X, double Y)> Les(string? tekst)
    {
        var punkter = new List<(double X, double Y)>();
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return punkter;
        }

        foreach (var del in tekst.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var xy = del.Split(',');
            if (xy.Length == 2
                && double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                && double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                punkter.Add((Math.Clamp(x, 0, 100), Math.Clamp(y, 0, 100)));
            }
        }

        return punkter;
    }

    public static string Skriv(IEnumerable<(double X, double Y)> punkter) =>
        string.Join(";", punkter.Select(p => $"{Tall(p.X)},{Tall(p.Y)}"));

    // SVG-sti gjennom alle punktene. Uten knekkpunkter blir det en rett strek.
    public static string Kurve(IReadOnlyList<(double X, double Y)> p)
    {
        if (p.Count < 2)
        {
            return "";
        }

        var d = $"M{Tall(p[0].X)} {Tall(p[0].Y)}";
        if (p.Count == 2)
        {
            return d + $" L{Tall(p[1].X)} {Tall(p[1].Y)}";
        }

        for (var i = 0; i < p.Count - 1; i++)
        {
            var p0 = p[Math.Max(0, i - 1)];
            var p1 = p[i];
            var p2 = p[i + 1];
            var p3 = p[Math.Min(p.Count - 1, i + 2)];
            var c1 = (X: p1.X + (p2.X - p0.X) / 6, Y: p1.Y + (p2.Y - p0.Y) / 6);
            var c2 = (X: p2.X - (p3.X - p1.X) / 6, Y: p2.Y - (p3.Y - p1.Y) / 6);
            d += $" C{Tall(c1.X)} {Tall(c1.Y)} {Tall(c2.X)} {Tall(c2.Y)} {Tall(p2.X)} {Tall(p2.Y)}";
        }

        return d;
    }

    private static string Tall(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
