using System.Globalization;
using System.Text;

namespace PortalItlock.Web.Models;

// Kryss og koblingspunkter på koblingsskjemaet:
//  - Der to streker krysser uten å være koblet, hopper den ene over den andre
//    i en liten halvmåne, så det er tydelig at de ikke henger sammen.
//  - Der en strek ender på en annen strek (eller tre streker møtes), tegnes en
//    prikk, så det er tydelig at de er koblet.
// Tegnes oppå strekene som et eget lag; selve strekene endres ikke.
public static class KoblingsKryss
{
    // Bredde i skjermpiksler (som på skjermen). Kant = omriss for hvit leder.
    // StartTeller/SluttTeller: om enden kan gi en prikk (ikke kabelenden der lederne begynner).
    public sealed record Linje(List<KoblingsStrekPunkt> Punkter, string Farge, double Bredde, bool StartTeller = true, bool SluttTeller = true, string? Kant = null);

    // Hopp: A og B er der halvmånen starter og slutter på strek Over. U1-U2 er
    // en kort bit av strek Under som tegnes på nytt i gapet.
    public sealed record Hopp(int Over, int Under, KoblingsStrekPunkt A, KoblingsStrekPunkt B, KoblingsStrekPunkt U1, KoblingsStrekPunkt U2, double R);

    public sealed record Prikk(int Linje, KoblingsStrekPunkt Punkt);

    // Størrelsene er i skjermpiksler, så halvmåner og prikker er like små
    // uansett zoom. pxPerProsent = hvor mange piksler 1 % av lerretets bredde er.
    private const double RadiusPx = 5;      // halvmånen (+ litt for tykke understreker)
    private const double StykkePx = 3.5;    // biten av understreken som tegnes på nytt
    private const double PrikkPx = 2.6;     // prikken der streker er koblet
    private const double Toleranse = 0.15;  // hvor nær en ende må være en strek for å regnes som koblet (prosent)
    public const double StandardPxPerProsent = 10;

    // Alle streker slik de tegnes, i rekkefølge. vifte gir kabelens ledere.
    public static List<Linje> Linjer(IEnumerable<KoblingsStrek> streker, Func<KoblingsStrek, KoblingsKabel.Vifte?> vifte, Func<string, List<KoblingsStrekPunkt>> punkter)
    {
        var res = new List<Linje>();
        foreach (var s in streker)
        {
            var p = punkter(s.PunkterJson);
            if (p.Count < 2)
            {
                continue;
            }

            if (s.ErKabel && vifte(s) is { } v)
            {
                res.Add(new Linje(p, KoblingsKabel.KabelFarge, 7, false, false));
                foreach (var l in v.Ledere)
                {
                    res.Add(new Linje(l.Punkter, KoblingsKabel.Hex(l.Leder.Farge), 2.2, false, true, l.Leder.Farge == "Hvit" ? "#9a948c" : null));
                }
            }
            else if (!s.ErKabel)
            {
                res.Add(new Linje(p, s.Farge, s.Tykkelse, Kant: KoblingsStrekTekst.ErHvit(s.Farge) ? KoblingsStrekTekst.HvitKant : null));
            }
        }
        return res;
    }

    // aspekt = høyde/bredde på lerretet, så halvmånene blir runde.
    public static (List<Hopp> Hopp, List<Prikk> Prikker) Finn(IReadOnlyList<Linje> linjer, double aspekt, double pxPerProsent = StandardPxPerProsent)
    {
        var Stykke = StykkePx / pxPerProsent;
        (double X, double Y) V(KoblingsStrekPunkt p) => (p.X, p.Y * aspekt);
        KoblingsStrekPunkt P(double x, double y) => new() { X = x, Y = y / aspekt };

        var hopp = new List<Hopp>();
        for (var i = 0; i < linjer.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                // Den tynneste hopper over; like tykke: den som er tegnet sist.
                var (over, under) = linjer[i].Bredde <= linjer[j].Bredde ? (i, j) : (j, i);
                var po = linjer[over].Punkter;
                var pu = linjer[under].Punkter;
                for (var a = 0; a < po.Count - 1; a++)
                {
                    var (ax, ay) = V(po[a]);
                    var (bx, by) = V(po[a + 1]);
                    double dx = bx - ax, dy = by - ay;
                    var lo = Math.Sqrt(dx * dx + dy * dy);
                    var r = (RadiusPx + linjer[under].Bredde * 0.4) / pxPerProsent;
                    if (lo < 2 * r + 0.1)
                    {
                        continue;
                    }

                    for (var b = 0; b < pu.Count - 1; b++)
                    {
                        var (cx, cy) = V(pu[b]);
                        var (ex, ey) = V(pu[b + 1]);
                        double fx = ex - cx, fy = ey - cy;
                        var lu = Math.Sqrt(fx * fx + fy * fy);
                        var nevner = dx * fy - dy * fx;
                        if (lu < 0.01 || Math.Abs(nevner) < 1e-9 * lo * lu)
                        {
                            continue;
                        }

                        var t = ((cx - ax) * fy - (cy - ay) * fx) / nevner;
                        var u = ((cx - ax) * dy - (cy - ay) * dx) / nevner;
                        // Må krysse inne på begge, med plass til halvmånen.
                        if (t * lo < r + 0.05 || (1 - t) * lo < r + 0.05 || u * lu < Toleranse || (1 - u) * lu < Toleranse)
                        {
                            continue;
                        }

                        double kx = ax + dx * t, ky = ay + dy * t;
                        double ux = dx / lo, uy = dy / lo;
                        // Fra venstre mot høyre (eller ovenfra og ned), så halvmånen alltid buer samme vei.
                        if (ux < -1e-9 || (Math.Abs(ux) <= 1e-9 && uy < 0))
                        {
                            ux = -ux;
                            uy = -uy;
                        }
                        double gx = fx / lu, gy = fy / lu;
                        hopp.Add(new Hopp(over, under,
                            P(kx - ux * r, ky - uy * r), P(kx + ux * r, ky + uy * r),
                            P(kx - gx * Stykke, ky - gy * Stykke), P(kx + gx * Stykke, ky + gy * Stykke), r));
                    }
                }
            }
        }

        // Halvmånene skal aldri gå over hverandre: står to kryss tett (på samme
        // strek, eller to streker som krysser den samme), krympes buene så de
        // får plass. Slik blir det små, separate buer også når man zoomer ut.
        if (hopp.Count > 1)
        {
            var senter = hopp.Select(h => ((h.A.X + h.B.X) / 2, (h.A.Y + h.B.Y) / 2 * aspekt)).ToList();
            var ny = new List<Hopp>(hopp.Count);
            for (var i = 0; i < hopp.Count; i++)
            {
                var h = hopp[i];
                var r = h.R;
                for (var j = 0; j < hopp.Count; j++)
                {
                    if (j != i)
                    {
                        r = Math.Min(r, Avst(senter[i].Item1, senter[i].Item2, senter[j].Item1, senter[j].Item2) * 0.42);
                    }
                }
                r = Math.Max(r, h.R * 0.25);
                if (r < h.R - 1e-9)
                {
                    var (cx, cy) = senter[i];
                    double ux = (h.B.X - h.A.X) / (2 * h.R), uy = (h.B.Y - h.A.Y) * aspekt / (2 * h.R);
                    h = h with { A = P(cx - ux * r, cy - uy * r), B = P(cx + ux * r, cy + uy * r), R = r };
                }
                ny.Add(h);
            }
            hopp = ny;
        }

        // Prikker: en ende som ligger på en annen strek (ikke på enden av den),
        // eller der tre eller flere ender møtes.
        var prikker = new List<Prikk>();
        var ender = new List<(int Linje, double X, double Y)>();
        for (var i = 0; i < linjer.Count; i++)
        {
            var p = linjer[i].Punkter;
            if (linjer[i].StartTeller)
            {
                var (x, y) = V(p[0]);
                ender.Add((i, x, y));
            }
            if (linjer[i].SluttTeller)
            {
                var (x, y) = V(p[^1]);
                ender.Add((i, x, y));
            }
        }

        void LeggTil(int linje, double x, double y)
        {
            if (!prikker.Any(q => Math.Abs(q.Punkt.X - x) < Toleranse && Math.Abs(q.Punkt.Y * aspekt - y) < Toleranse))
            {
                prikker.Add(new Prikk(linje, P(x, y)));
            }
        }

        foreach (var (i, x, y) in ender)
        {
            for (var j = 0; j < linjer.Count; j++)
            {
                if (j == i)
                {
                    continue;
                }
                var p = linjer[j].Punkter;
                var (sx, sy) = V(p[0]);
                var (tx, ty) = V(p[^1]);
                if (Avst(x, y, sx, sy) < Toleranse || Avst(x, y, tx, ty) < Toleranse)
                {
                    continue;
                }
                for (var b = 0; b < p.Count - 1; b++)
                {
                    var (cx, cy) = V(p[b]);
                    var (ex, ey) = V(p[b + 1]);
                    if (AvstTilStykke(x, y, cx, cy, ex, ey) < Toleranse)
                    {
                        LeggTil(i, x, y);
                        break;
                    }
                }
            }

            if (ender.Count(e => Avst(e.X, e.Y, x, y) < Toleranse) >= 3)
            {
                LeggTil(i, x, y);
            }
        }

        return (hopp, prikker);
    }

    // SVG for laget oppå strekene. yFaktor gjør om prosent-Y til viewBox-Y.
    // breddeFaktor gjør om skjermpiksler til viewBox-enheter (1 og
    // ikkeSkalerende = true på skjermen, der bredden er i piksler).
    public static string Svg(IReadOnlyList<Linje> linjer, double aspekt, double yFaktor = 1, double breddeFaktor = 1, bool ikkeSkalerende = true, string bakgrunn = "#fff", double pxPerProsent = StandardPxPerProsent)
    {
        if (linjer.Count < 2)
        {
            return "";
        }

        var (hopp, prikker) = Finn(linjer, aspekt, pxPerProsent);
        var PrikkRadius = PrikkPx / pxPerProsent;
        if (hopp.Count == 0 && prikker.Count == 0)
        {
            return "";
        }

        static string I(double v) => Math.Round(v, 3).ToString(CultureInfo.InvariantCulture);
        string Pt(KoblingsStrekPunkt p) => $"{I(p.X)},{I(p.Y * yFaktor)}";
        var ns = ikkeSkalerende ? " vector-effect='non-scaling-stroke'" : "";
        string B(double px) => I(px * breddeFaktor);

        var sb = new StringBuilder("<g class='ks-kryss' pointer-events='none'>");
        var buer = SlaSammen(hopp, aspekt, 1.5 / pxPerProsent);
        // Gapet i overstreken (én gang per bue, også når flere kryss er slått sammen).
        foreach (var (over, a, bSlutt, _) in buer)
        {
            var o = linjer[over];
            sb.Append($"<line x1='{I(a.X)}' y1='{I(a.Y * yFaktor)}' x2='{I(bSlutt.X)}' y2='{I(bSlutt.Y * yFaktor)}' stroke='{bakgrunn}' stroke-width='{B(Math.Max(o.Bredde, o.Kant is null ? 0 : 3.6) + 2.5)}'{ns} />");
        }
        // Biten av understreken som går under buen.
        foreach (var h in hopp)
        {
            var u = linjer[h.Under];
            if (u.Kant is not null)
            {
                sb.Append($"<line x1='{I(h.U1.X)}' y1='{I(h.U1.Y * yFaktor)}' x2='{I(h.U2.X)}' y2='{I(h.U2.Y * yFaktor)}' stroke='{u.Kant}' stroke-width='{B(3.6)}'{ns} />");
            }
            sb.Append($"<line x1='{I(h.U1.X)}' y1='{I(h.U1.Y * yFaktor)}' x2='{I(h.U2.X)}' y2='{I(h.U2.Y * yFaktor)}' stroke='{u.Farge}' stroke-width='{B(u.Bredde)}'{ns} />");
        }
        foreach (var (over, a, bSlutt, r) in buer)
        {
            var o = linjer[over];
            double dx = bSlutt.X - a.X, dy = (bSlutt.Y - a.Y) * aspekt;
            var halv = Math.Sqrt(dx * dx + dy * dy) / 2;
            // Langs streken: halve lengden. På tvers: like høy som en enkelt halvmåne.
            var (rx, ry) = Math.Abs(dx) >= Math.Abs(dy) ? (halv, r) : (r, halv);
            var bue = $"M{Pt(a)} A{I(rx)},{I(ry / aspekt * yFaktor)} 0 0 1 {Pt(bSlutt)}";
            if (o.Kant is not null)
            {
                sb.Append($"<path d='{bue}' fill='none' stroke='{o.Kant}' stroke-width='{B(3.6)}' stroke-linecap='round'{ns} />");
            }
            sb.Append($"<path d='{bue}' fill='none' stroke='{o.Farge}' stroke-width='{B(o.Bredde)}' stroke-linecap='round'{ns} />");
        }
        foreach (var p in prikker)
        {
            var farge = linjer[p.Linje].Farge;
            if (string.Equals(farge, "#ffffff", StringComparison.OrdinalIgnoreCase) || string.Equals(farge, "#fff", StringComparison.OrdinalIgnoreCase))
            {
                farge = "#2a2622";
            }
            sb.Append($"<ellipse cx='{I(p.Punkt.X)}' cy='{I(p.Punkt.Y * yFaktor)}' rx='{I(PrikkRadius)}' ry='{I(PrikkRadius / aspekt * yFaktor)}' fill='{farge}' />");
        }
        sb.Append("</g>");
        return sb.ToString();
    }

    // Hopp på samme strek som overlapper eller nesten møtes, slås sammen til ett.
    public static List<(int Over, KoblingsStrekPunkt A, KoblingsStrekPunkt B, double R)> SlaSammen(List<Hopp> hopp, double aspekt, double mellomrom = 0.15)
    {
        var res = new List<(int Over, KoblingsStrekPunkt A, KoblingsStrekPunkt B, double R)>();
        foreach (var gruppe in hopp.GroupBy(h => h.Over))
        {
            // Hoppene på en strek grupperes per rett linje (samme retning og samme linje),
            // og sorteres langs den.
            var igjen = gruppe.ToList();
            while (igjen.Count > 0)
            {
                var f = igjen[0];
                double ux = f.B.X - f.A.X, uy = (f.B.Y - f.A.Y) * aspekt;
                var l = Math.Sqrt(ux * ux + uy * uy);
                ux /= l;
                uy /= l;
                double Langs(KoblingsStrekPunkt p) => (p.X - f.A.X) * ux + (p.Y - f.A.Y) * aspekt * uy;
                double Tvers(KoblingsStrekPunkt p) => Math.Abs((p.X - f.A.X) * uy - (p.Y - f.A.Y) * aspekt * ux);
                var linje = igjen.Where(h => Tvers(h.A) < 0.05 && Tvers(h.B) < 0.05).OrderBy(h => Langs(h.A)).ToList();
                igjen.RemoveAll(linje.Contains);

                var (a, b, r) = (linje[0].A, linje[0].B, linje[0].R);
                foreach (var h in linje.Skip(1))
                {
                    if (Langs(h.A) <= Langs(b) + mellomrom)
                    {
                        if (Langs(h.B) > Langs(b))
                        {
                            b = h.B;
                        }
                        r = Math.Max(r, h.R);
                        continue;
                    }
                    res.Add((gruppe.Key, a, b, r));
                    (a, b, r) = (h.A, h.B, h.R);
                }
                res.Add((gruppe.Key, a, b, r));
            }
        }
        return res;
    }

    private static double Avst(double ax, double ay, double bx, double by) =>
        Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));

    private static double AvstTilStykke(double px, double py, double ax, double ay, double bx, double by)
    {
        double dx = bx - ax, dy = by - ay;
        var l2 = dx * dx + dy * dy;
        var t = l2 == 0 ? 0 : Math.Clamp(((px - ax) * dx + (py - ay) * dy) / l2, 0, 1);
        return Avst(px, py, ax + dx * t, ay + dy * t);
    }
}
