using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Stilarkene har mange hardkodede varme farger (brun, beige, varmgrå) fra
// standardtemaet. For temaer med Omfarging (f.eks. Marineblå) serveres en
// omfarget kopi av stilarkene på /tema/{id}/{fil}: alle varme farger flyttes
// til temaets fargetone med samme lyshet, mens gule/oransje statusfarger
// (advarsel) og grå/rød/grønn beholdes. I tillegg ensrettes ikonbrikker og
// fliser som ellers har én farge hver (Hjem, prosjektoversikt, lager).
public static class TemaCss
{
    // Stilarkene som kan omfarges (kassa.css har sitt eget, mørke uttrykk).
    public static readonly IReadOnlySet<string> Filer = new HashSet<string>(StringComparer.Ordinal)
    {
        "app.css", "montorapp.css", "drift.css", "PortalItlock.Web.styles.css"
    };

    private static readonly ConcurrentDictionary<string, (DateTimeOffset Endret, string Css)> Cache = new();

    private static readonly Regex Hex6 = new(@"#([0-9a-fA-F]{6})\b", RegexOptions.Compiled);
    private static readonly Regex Hex3 = new(@"#([0-9a-fA-F]{3})\b", RegexOptions.Compiled);
    private static readonly Regex Rgb = new(@"rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)", RegexOptions.Compiled);

    // Lenken til et stilark for organisasjonens tema: omfarget kopi når
    // temaet har Omfarging, ellers den vanlige filen.
    public static string Href(OrganisasjonTema? tema, string fil, int versjon) =>
        tema?.Omfarging is not null && Filer.Contains(fil)
            ? $"tema/{tema.Id}/{fil}?v={versjon}"
            : $"{fil}?v={versjon}";

    public static string? HentOmfarget(IFileProvider filer, OrganisasjonTema tema, string fil)
    {
        if (tema.Omfarging is not int tone || !Filer.Contains(fil))
        {
            return null;
        }

        var info = filer.GetFileInfo(fil);
        if (!info.Exists)
        {
            return null;
        }

        var nokkel = tema.Id + "/" + fil;
        if (Cache.TryGetValue(nokkel, out var lagret) && lagret.Endret == info.LastModified)
        {
            return lagret.Css;
        }

        using var strom = info.CreateReadStream();
        using var leser = new StreamReader(strom);
        var css = Omfarg(leser.ReadToEnd(), tone);
        if (fil == "app.css")
        {
            css += Ensretting(tema);
        }

        Cache[nokkel] = (info.LastModified, css);
        return css;
    }

    public static string Omfarg(string css, int tone)
    {
        css = Hex6.Replace(css, m =>
        {
            var x = m.Groups[1].Value;
            var ny = Flytt(Byte(x, 0), Byte(x, 2), Byte(x, 4), tone);
            return ny is null ? m.Value : "#" + Hex(ny.Value);
        });
        css = Hex3.Replace(css, m =>
        {
            var x = m.Groups[1].Value;
            var ny = Flytt(Byte3(x[0]), Byte3(x[1]), Byte3(x[2]), tone);
            return ny is null ? m.Value : "#" + Hex(ny.Value);
        });
        return Rgb.Replace(css, m =>
        {
            var ny = Flytt(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), tone);
            if (ny is null)
            {
                return m.Value;
            }

            var start = m.Value.IndexOf('(');
            return m.Value[..(start + 1)] + $"{ny.Value.R}, {ny.Value.G}, {ny.Value.B}";
        });
    }

    // Flytter en varm farge til fargetonen, eller null når den skal beholdes.
    public static (int R, int G, int B)? Flytt(int r, int g, int b, int tone)
    {
        var (h, s, l) = TilHsl(r, g, b);
        if (h < 12 || h > 56 || s < .025 || l > .985 || l < .04)
        {
            return null;
        }

        // Gule/oransje statusfarger (advarsel) og deres lyse bakgrunner beholdes.
        if (h >= 34 && s >= .62 && l > .25 && l < .62) return null;
        if (h >= 34 && s >= .6 && l >= .8) return null;

        var ns = s > .25 ? Math.Min(.68, s * 1.7) : Math.Min(.35, s * 1.3 + .04);
        var nl = l > .3 && l < .6 ? l * .9 : l;
        return FraHsl(tone, ns, nl);
    }

    // Én farge på ikonbrikker og fliser i stedet for regnbuen fra standardtemaet.
    private static string Ensretting(OrganisasjonTema t) => $$"""

        /* ---------- Ensretting for omfarget tema ({{t.Id}}) ---------- */
        html:not([data-theme="dark"]) .fh-flis { background: #fff !important; border: 1px solid var(--itlock-border) !important; box-shadow: 0 1px 2px rgba(15, 27, 51, .04) !important; }
        html:not([data-theme="dark"]) .fh-flis:hover { border-color: {{t.Primaer}} !important; }
        html:not([data-theme="dark"]) :is(.fh-ikb, .ov-rad-ikon, .lg-lagerikon) { background: {{t.PrimaerLys}} !important; color: {{t.Primaer}} !important; }
        html:not([data-theme="dark"]) :is(.fh-ikb, .ov-rad-ikon, .lg-lagerikon) svg { color: {{t.Primaer}} !important; }
        html:not([data-theme="dark"]) .kanban-kolonne { background: #F6F8FB !important; border: 1px solid var(--itlock-border) !important; }
        html:not([data-theme="dark"]) :is(.fh-kort, .kort, .card) { border-color: var(--itlock-border); }
        html[data-tema-meny="mork"] .sidebar .nav-link svg { color: inherit !important; }
        html[data-tema-meny="mork"] .sidebar .nav-link svg * { stroke: currentColor; }
        html[data-tema-meny="mork"] .sidebar .bi-house-door-fill-nav-menu { filter: brightness(0) invert(.82); }
        html[data-tema-meny="mork"] .sidebar a.active .bi-house-door-fill-nav-menu { filter: brightness(0) invert(1); }
        """;

    private static int Byte(string x, int i) => int.Parse(x.AsSpan(i, 2), NumberStyles.HexNumber);
    private static int Byte3(char c) => int.Parse(new string(c, 2), NumberStyles.HexNumber);
    private static string Hex((int R, int G, int B) c) => $"{c.R:x2}{c.G:x2}{c.B:x2}";

    private static (double H, double S, double L) TilHsl(int ri, int gi, int bi)
    {
        double r = ri / 255d, g = gi / 255d, b = bi / 255d;
        var mx = Math.Max(r, Math.Max(g, b));
        var mn = Math.Min(r, Math.Min(g, b));
        var l = (mx + mn) / 2;
        if (mx == mn)
        {
            return (0, 0, l);
        }

        var d = mx - mn;
        var s = l > .5 ? d / (2 - mx - mn) : d / (mx + mn);
        var h = mx == r ? (g - b) / d + (g < b ? 6 : 0) : mx == g ? (b - r) / d + 2 : (r - g) / d + 4;
        return (h * 60, s, l);
    }

    private static (int R, int G, int B) FraHsl(double h, double s, double l)
    {
        h /= 360;
        var q = l < .5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        static double Del(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1d / 6) return p + (q - p) * 6 * t;
            if (t < 1d / 2) return q;
            if (t < 2d / 3) return p + (q - p) * (2d / 3 - t) * 6;
            return p;
        }

        return ((int)Math.Round(Del(p, q, h + 1d / 3) * 255), (int)Math.Round(Del(p, q, h) * 255), (int)Math.Round(Del(p, q, h - 1d / 3) * 255));
    }
}
