using System.Text.RegularExpressions;
using static PortalItlock.Web.Services.PdfPlan;

namespace PortalItlock.Web.Services;

// Finner dører på en plantegning (vektor-PDF):
//  - dørmerke: Dør ID-kode (f.eks. «ID21») + mål («9x21M» = 900 × 2100)
//  - dørbue: kvartsirkel der senteret er hengselet; bladstreken viser åpen stilling
//  - høyre/venstre: sett fra siden døren slår mot - hengsler på din høyre side = H
//  - dør til: romstempelet (navn, nummer, «7.4 m²») i rommet døren slår inn i
public static partial class PlanDorLeser
{
    public sealed record FunnetDor(double PosX, double PosY, string? DorIdKode, int? Bredde, int? Hoyde, string? Slag,
        string? RomNavn, string? RomNr, bool ToFloyet, bool HarMerke, bool HarBue, List<string> Usikkert);

    public sealed record Bue(P Senter, double Radius, P A, P B);
    private sealed record Rom(P Midt, string? Navn, string? Nr);

    [GeneratedRegex(@"^(ID|YD|BD|IDS|VSD|YDP|SD|PD|BRD)[-_]?\d{1,3}[A-ZÆØÅ0-9-]*$", RegexOptions.IgnoreCase)]
    private static partial Regex KodeRegex();

    [GeneratedRegex(@"^(\d{1,2})[x×X\*](\d{2})M?$")]
    private static partial Regex ModulRegex();

    [GeneratedRegex(@"^(\d{3,4})[x×X\*](\d{3,4})$")]
    private static partial Regex MmRegex();

    // «1 090x2 690» / «990x2 390»: mål med mellomrom som tusenskille. PDF-en
    // deler det i flere ord («1», «090x2», «690»), så de settes sammen først.
    [GeneratedRegex(@"(?:^|\s)(?:(\d)\s)?(\d{3})\s?[x×]\s?(\d)\s(\d{3})(?:\s|$)")]
    private static partial Regex DeltMaalRegex();

    [GeneratedRegex(@"^\d{1,4}([.,]\d{1,2})?(m²|m2)?$")]
    private static partial Regex ArealRegex();

    [GeneratedRegex(@"^[A-ZÆØÅ][A-ZÆØÅa-zæøå/.&-]{1,30}$")]
    private static partial Regex NavnRegex();

    [GeneratedRegex(@"\d{2,}")]
    private static partial Regex HarTall();

    private static (int B, int H)? Maal(string t)
    {
        var m = ModulRegex().Match(t);
        if (m.Success) return (int.Parse(m.Groups[1].Value) * 100, int.Parse(m.Groups[2].Value) * 100);
        m = MmRegex().Match(t);
        if (m.Success) return (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
        return null;
    }

    // Mål skrevet over flere ord rett etter dørmerket (i rekkefølgen i PDF-en).
    private static (int B, int H)? DeltMaal(Side side, Ord m)
    {
        var i = side.Ord.IndexOf(m);
        if (i < 0) return null;
        bool Naer(Ord o) => P.Avst(o.Midt, m.Midt) < m.Storrelse * 5;
        var etter = side.Ord.Skip(i + 1).Take(4).TakeWhile(Naer).Select(o => o.Tekst.Trim()).ToList();
        var tekst = string.Join(" ", etter);
        // Rotert tekst: bredden kan stå rett foran merket («990 | ID-01-2 | x2 | 390»).
        if (tekst.StartsWith('x') && i > 0 && Naer(side.Ord[i - 1]))
        {
            tekst = side.Ord[i - 1].Tekst.Trim() + tekst;
        }
        var t = DeltMaalRegex().Match(tekst);
        if (!t.Success) return null;
        var b = int.Parse(t.Groups[2].Value) + (t.Groups[1].Success ? int.Parse(t.Groups[1].Value) * 1000 : 0);
        var h = int.Parse(t.Groups[3].Value) * 1000 + int.Parse(t.Groups[4].Value);
        return b is >= 400 and <= 3500 && h is >= 1500 and <= 4000 ? (b, h) : null;
    }

    public static List<Bue> FinnBuer(Side side)
    {
        var ut = new List<Bue>();
        foreach (var l in side.Linjer.Where(l => l.Count >= 4))
        {
            // del opp lukkede figurer: ta bare den sammenhengende buedelen hvis figuren også har rette streker
            foreach (var del in BueDeler(l))
            {
                if (TilpassBue(del) is { } b) ut.Add(b);
            }
        }
        // fjern doble (samme bue tegnet to ganger)
        var unike = ut.Where((b, i) => !ut.Take(i).Any(x => P.Avst(x.Senter, b.Senter) < b.Radius * 0.05 && Math.Abs(x.Radius - b.Radius) < b.Radius * 0.05)).ToList();
        // En hel sirkel (f.eks. stiplet snusirkel) tegnes som fire kvartbuer med samme
        // senter. Det er ikke en dør: en dørbue står alene rundt hengselet.
        bool Samme(Bue x, Bue b) => P.Avst(x.Senter, b.Senter) < b.Radius * 0.05 && Math.Abs(x.Radius - b.Radius) < b.Radius * 0.05;
        bool Lik(P a, P b, double r) => P.Avst(a, b) < r * 0.05;
        return unike.Where(b => !unike.Any(x => x != b && Samme(x, b)
            && (Lik(x.A, b.A, b.Radius) || Lik(x.A, b.B, b.Radius) || Lik(x.B, b.A, b.Radius) || Lik(x.B, b.B, b.Radius)))).ToList();
    }

    // Deler en punktliste der retningen skifter brått (hjørne), så en bue med rette biter rundt også blir funnet.
    private static IEnumerable<List<P>> BueDeler(List<P> l)
    {
        var del = new List<P> { l[0] };
        for (var i = 1; i < l.Count; i++)
        {
            if (del.Count >= 2)
            {
                var v1 = del[^1] - del[^2];
                var v2 = l[i] - del[^1];
                if (v1.Len > 0 && v2.Len > 0)
                {
                    var cos = (v1.X * v2.X + v1.Y * v2.Y) / (v1.Len * v2.Len);
                    if (cos < 0.85)
                    {
                        if (del.Count >= 4) yield return del;
                        del = [del[^1]];
                    }
                }
            }
            del.Add(l[i]);
        }
        if (del.Count >= 4) yield return del;
    }

    private static Bue? TilpassBue(List<P> pts)
    {
        var a = pts[0];
        var m = pts[pts.Count / 2];
        var b = pts[^1];
        // sirkel gjennom tre punkter
        var d = 2 * (a.X * (m.Y - b.Y) + m.X * (b.Y - a.Y) + b.X * (a.Y - m.Y));
        if (Math.Abs(d) < 1e-6) return null;
        double a2 = a.X * a.X + a.Y * a.Y, m2 = m.X * m.X + m.Y * m.Y, b2 = b.X * b.X + b.Y * b.Y;
        var c = new P((a2 * (m.Y - b.Y) + m2 * (b.Y - a.Y) + b2 * (a.Y - m.Y)) / d,
                      (a2 * (b.X - m.X) + m2 * (a.X - b.X) + b2 * (m.X - a.X)) / d);
        var r = P.Avst(c, a);
        if (r < 4 || r > 300) return null;
        if (pts.Any(p => Math.Abs(P.Avst(c, p) - r) > r * 0.06)) return null;
        // vinkelspenn
        double span = 0;
        for (var i = 1; i < pts.Count; i++)
        {
            var v1 = Math.Atan2(pts[i - 1].Y - c.Y, pts[i - 1].X - c.X);
            var v2 = Math.Atan2(pts[i].Y - c.Y, pts[i].X - c.X);
            var dv = v2 - v1;
            while (dv > Math.PI) dv -= 2 * Math.PI;
            while (dv < -Math.PI) dv += 2 * Math.PI;
            span += dv;
        }
        var grader = Math.Abs(span) * 180 / Math.PI;
        return grader is >= 70 and <= 110 ? new Bue(c, r, a, b) : null;
    }

    // Hvilket endepunkt av buen er døren i åpen stilling? Der det går en strek (dørbladet) fra hengselet.
    private static P? AapenEnde(Bue bue, Side side)
    {
        var r = bue.Radius;
        // Dørbladet i åpen stilling: en strek (eller siden i et tynt rektangel)
        // som går fra hengselet langs radien ut mot buens ende. Rektangelet kan
        // ligge litt forskjøvet fra hengselpunktet (bladets tykkelse).
        var streker = side.Streker.Select(s => (s.A, s.B))
            .Concat(side.Linjer.Where(l => l.Any(p => P.Avst(p, bue.Senter) < r * 0.3))
                .SelectMany(l => l.Zip(l.Skip(1), (p, q) => (p, q))))
            .ToList();
        bool Blad(P ende, double maks)
        {
            var u = (ende - bue.Senter) * (1 / r);
            var nn = new P(-u.Y, u.X);
            foreach (var (sa, sb) in streker)
            {
                var va = sa - bue.Senter;
                var vb = sb - bue.Senter;
                double ta = va.X * u.X + va.Y * u.Y, tb = vb.X * u.X + vb.Y * u.Y;
                double da = va.X * nn.X + va.Y * nn.Y, db = vb.X * nn.X + vb.Y * nn.Y;
                var tMin = Math.Min(ta, tb);
                var tMax = Math.Max(ta, tb);
                if (Math.Abs(da) > r * 0.15 || Math.Abs(db) > r * 0.15) continue;
                if (tMin < -r * 0.25 || tMin > r * 0.25) continue;
                if (tMax < r * 0.75 || tMax > maks) continue;
                return true;
            }
            return false;
        }
        // Sikrest: dørbladet ender nøyaktig der buen slutter. Karm og terskel
        // langs veggen går litt forbi buens ende, og vegger går videre.
        bool EnderPaBuen(P ende)
        {
            var u = (ende - bue.Senter) * (1 / r);
            var nn = new P(-u.Y, u.X);
            foreach (var (sa, sb) in streker)
            {
                foreach (var (fra, til) in new[] { (sa, sb), (sb, sa) })
                {
                    if (P.Avst(til, ende) > r * 0.03) continue;
                    var v = fra - bue.Senter;
                    var t = v.X * u.X + v.Y * u.Y;
                    var d = v.X * nn.X + v.Y * nn.Y;
                    if (Math.Abs(d) <= r * 0.15 && Math.Abs(t) <= r * 0.15) return true;
                }
            }
            return false;
        }
        var ea = EnderPaBuen(bue.A);
        var eb = EnderPaBuen(bue.B);
        if (ea != eb) return ea ? bue.A : bue.B;

        // Ellers: en strek omtrent like lang som døra (veggstreker er lengre).
        var a = Blad(bue.A, r * 1.3);
        var b = Blad(bue.B, r * 1.3);
        if (a == b)
        {
            a = Blad(bue.A, double.MaxValue);
            b = Blad(bue.B, double.MaxValue);
        }
        if (a == b) return null;
        return a ? bue.A : bue.B;
    }

    private static List<Rom> FinnRom(Side side, HashSet<Ord> brukt)
    {
        var rom = new List<Rom>();
        for (var i = 0; i < side.Ord.Count; i++)
        {
            var o = side.Ord[i];
            var t = o.Tekst.Trim();
            var erAreal = (t.EndsWith("m²") || t.EndsWith("m2")) && ArealRegex().IsMatch(t);
            if (!erAreal && ArealRegex().IsMatch(t))
            {
                erAreal = side.Ord.Any(x => (x.Tekst is "m²" or "m2" or "m2." or "m²." ) && P.Avst(x.Midt, o.Midt) < o.Storrelse * 4);
            }
            if (!erAreal || t.Contains('x')) continue;
            var radius = o.Storrelse * 6;
            var naer = side.Ord.Where(x => x != o && !brukt.Contains(x) && P.Avst(x.Midt, o.Midt) < radius).OrderBy(x => P.Avst(x.Midt, o.Midt)).ToList();
            var navn = naer.FirstOrDefault(x => NavnRegex().IsMatch(x.Tekst) && !HarTall().IsMatch(x.Tekst) && x.Tekst is not ("m²" or "m2") && !KodeRegex().IsMatch(x.Tekst));
            var nr = naer.FirstOrDefault(x => HarTall().IsMatch(x.Tekst) && x.Tekst.Any(char.IsLetter) && !KodeRegex().IsMatch(x.Tekst) && Maal(x.Tekst) is null && !ArealRegex().IsMatch(x.Tekst));
            if (navn is null && nr is null) continue;
            var nrTekst = nr?.Tekst;
            if (nrTekst is not null && nrTekst.Contains('/')) nrTekst = nrTekst[(nrTekst.LastIndexOf('/') + 1)..];
            rom.Add(new Rom(o.Midt, navn?.Tekst, nrTekst));
        }
        return rom;
    }

    public static List<FunnetDor> Les(Side side, IEnumerable<string> kjenteKoder)
    {
        var kjente = kjenteKoder.Select(DorTypeSkjemaLeser.Norm).Where(k => k.Length > 1).ToHashSet();
        var merker = side.Ord.Where(o => kjente.Contains(DorTypeSkjemaLeser.Norm(o.Tekst)) || KodeRegex().IsMatch(o.Tekst.Trim())).ToList();
        var maalOrd = side.Ord.Where(o => Maal(o.Tekst.Trim()) is not null).ToList();
        // Dørmerker kobles til nærmeste bue. Buer uten merke foreslås bare når
        // dørbladet er tegnet (ellers er det ofte noe annet enn en dør).
        var buer = FinnBuer(side);
        var rom = FinnRom(side, [.. merker, .. maalOrd]);
        var brukteBuer = new HashSet<Bue>();
        var ut = new List<FunnetDor>();

        // Dørmerker med nærmeste bue
        var par = merker.SelectMany(m => buer.Select(b => (m, b, d: P.Avst(m.Midt, Aapning(b)))))
            .Where(x => x.d < x.b.Radius * 3.5)
            .OrderBy(x => x.d).ToList();
        var merkeTilBue = new Dictionary<Ord, Bue>();
        foreach (var (m, b, _) in par)
        {
            if (merkeTilBue.ContainsKey(m) || brukteBuer.Contains(b)) continue;
            merkeTilBue[m] = b;
            brukteBuer.Add(b);
        }

        foreach (var m in merker)
        {
            var usikkert = new List<string>();
            var maalNaer = maalOrd.Where(x => P.Avst(x.Midt, m.Midt) < Math.Max(m.Storrelse, x.Storrelse) * 5)
                .OrderBy(x => P.Avst(x.Midt, m.Midt)).FirstOrDefault();
            var maal = maalNaer is null ? DeltMaal(side, m) : Maal(maalNaer.Tekst.Trim());
            if (maal is null) usikkert.Add("mål ikke funnet");
            merkeTilBue.TryGetValue(m, out var bue);
            // Uten dørbue (f.eks. tømrerplan uten slag): dørmerket står i åpningen,
            // så døra legges midt i merket. Høyre/venstre må settes for hånd.
            var merkeMidt = DeltMaalOrd(side, m) is { } mo ? (m.Midt + mo.Midt) * 0.5 : m.Midt;
            var (pos, slag, romPunkt, toFloyet) = bue is null ? (merkeMidt, (string?)null, merkeMidt, false) : Analyser(bue, side, buer, brukteBuer, usikkert);
            if (bue is null) usikkert.Add("slag ikke tegnet");
            var r = NaermesteRom(romPunkt, rom, usikkert);
            ut.Add(new FunnetDor(pos.X / side.Bredde * 100, pos.Y / side.Hoyde * 100, m.Tekst.Trim().ToUpperInvariant(),
                maal?.B, maal?.H, slag, r?.Navn, r?.Nr, toFloyet, true, bue is not null, usikkert));
        }

        // Dørbuer uten merke (foreslås, men er ikke huket av)
        foreach (var b in buer.Where(b => !brukteBuer.Contains(b) && AapenEnde(b, side) is not null))
        {
            var usikkert = new List<string> { "uten dørmerke" };
            var (pos, slag, romPunkt, toFloyet) = Analyser(b, side, buer, brukteBuer, usikkert);
            var r = NaermesteRom(romPunkt, rom, usikkert);
            ut.Add(new FunnetDor(pos.X / side.Bredde * 100, pos.Y / side.Hoyde * 100, null, null, null, slag, r?.Navn, r?.Nr, toFloyet, false, true, usikkert));
        }
        return ut.OrderBy(d => Math.Round(d.PosY / 5)).ThenBy(d => d.PosX).ToList();
    }

    private static P Aapning(Bue b) => (b.A + b.B) * 0.5;

    // Ordet med «x» i målet rett etter merket, for å finne midten av dørmerket.
    private static Ord? DeltMaalOrd(Side side, Ord m)
    {
        var i = side.Ord.IndexOf(m);
        return i < 0 ? null : side.Ord.Skip(i + 1).Take(3)
            .FirstOrDefault(o => o.Tekst.Contains('x') && P.Avst(o.Midt, m.Midt) < m.Storrelse * 5);
    }

    private static (P Pos, string? Slag, P RomPunkt, bool ToFloyet) Analyser(Bue bue, Side side, List<Bue> alle, HashSet<Bue> brukte, List<string> usikkert)
    {
        var aapen = AapenEnde(bue, side);
        if (aapen is null)
        {
            usikkert.Add("slagretning usikker");
            return (Aapning(bue), null, Aapning(bue), false);
        }
        var po = aapen.Value;
        var pc = P.Avst(po, bue.A) < 1e-6 ? bue.B : bue.A;
        var c = bue.Senter;
        var n = (po - c) * (1 / Math.Max(1e-6, P.Avst(po, c)));
        var midt = (c + pc) * 0.5;
        // stå på slagsiden og se mot døren
        var f = n * -1;
        var hoyre = new P(-f.Y, f.X);
        var h = c - midt;
        var slag = h.X * hoyre.X + h.Y * hoyre.Y > 0 ? "H" : "V";

        // tofløyet: en annen bue som lukker mot samme punkt (også med ulik bredde,
        // f.eks. 1390 = stor fløy + liten sidefløy). Døra legges midt mellom hengslene.
        var partner = alle.FirstOrDefault(x => x != bue && x.Radius > bue.Radius * 0.2 && x.Radius < bue.Radius * 5
            && (P.Avst(x.A, pc) < bue.Radius * 0.15 || P.Avst(x.B, pc) < bue.Radius * 0.15));
        if (partner is not null)
        {
            brukte.Add(partner);
            var mellom = (c + partner.Senter) * 0.5;
            return (mellom, null, mellom + n * (bue.Radius * 0.6), true);
        }
        return (midt, slag, midt + n * (bue.Radius * 0.6), false);
    }

    private static Rom? NaermesteRom(P punkt, List<Rom> rom, List<string> usikkert)
    {
        if (rom.Count == 0) return null;
        var sortert = rom.OrderBy(r => P.Avst(r.Midt, punkt)).ToList();
        if (sortert.Count > 1 && P.Avst(sortert[1].Midt, punkt) < P.Avst(sortert[0].Midt, punkt) * 1.2)
        {
            usikkert.Add("rom usikkert");
        }
        return sortert[0];
    }
}
