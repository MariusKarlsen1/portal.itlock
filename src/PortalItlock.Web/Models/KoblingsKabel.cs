using System.Globalization;
using System.Text.Json;

namespace PortalItlock.Web.Models;

// Én leder i en flerlederkabel. Farge er et fargenavn fra KoblingsKabel.Farger,
// slik at kabellisten kan skrive "Rød" i stedet for en hex-kode.
public sealed class KoblingsLeder
{
    public string Farge { get; set; } = "Sort";
    public bool Stiplet { get; set; }
    public string Funksjon { get; set; } = "";

    // Kabeljournal: hvor lederen starter (klemme) og hvor den skal.
    public int? Par { get; set; }
    public string Klemme { get; set; } = "";
    public string Til { get; set; } = "";
    public string Merknad { get; set; } = "";

    // Egen vei for lederen i hver ende når montøren har dratt den (prosentpunkter
    // fra der lederen forlater kabelen og ut til der den kobles). Null = standard vifte.
    public List<KoblingsStrekPunkt>? Start { get; set; }
    public List<KoblingsStrekPunkt>? Slutt { get; set; }

    // Koblet til et koblingspunkt: veien legges automatisk (ryddig, symmetrisk og
    // uten kryssinger) fra kabelen til siste punkt i Start/Slutt.
    public bool StartAuto { get; set; }
    public bool SluttAuto { get; set; }
}

// Flerlederkabel på koblingsskjema: en tykk sort kabel som deler seg i
// fargede ledere rett før komponenten i hver ende. Geometrien regnes ut her
// slik at lerretet og PDF-en tegner kabelen likt.
public static class KoblingsKabel
{
    public const string KabelFarge = "#1d1d1b";
    public const int MaksLedere = 12;

    public static readonly IReadOnlyList<(string Navn, string Hex)> Farger =
    [
        ("Rød", "#d62828"), ("Sort", "#1d1d1b"), ("Gul", "#f2c200"), ("Grønn", "#1e9e4a"),
        ("Blå", "#1f6fd1"), ("Hvit", "#d9d9d9"), ("Brun", "#8a5a34"), ("Oransje", "#f08a24"),
        ("Grå", "#8c8c8c"), ("Lilla", "#7b4bb7"), ("Rosa", "#e86aa6"), ("Turkis", "#1aa6a6"),
    ];

    // Ferdige kabeltyper i panelet. Parkabler nummererer lederne i par.
    public sealed record Mal(string Navn, int Antall, bool Par);

    public static readonly IReadOnlyList<Mal> Maler =
    [
        new("2-leder", 2, false), new("4-leder", 4, false), new("6-leder", 6, false),
        new("8-leder", 8, false), new("12-leder", 12, false),
        new("2 par", 4, true), new("4 par", 8, true), new("6 par", 12, true),
    ];

    // Vanlig rekkefølge for en adgangskontrollkabel; montøren endrer fritt.
    private static readonly string[] StandardFunksjon =
        ["+12V", "0V", "Data1", "Data0", "LED", "Summer", "Lås +", "Lås −", "Kontakt", "Kontakt", "Reserve", "Reserve"];

    public static string Hex(string? navn) =>
        Farger.FirstOrDefault(f => f.Navn == navn).Hex ?? KabelFarge;

    public static List<KoblingsLeder> Les(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }
        try
        {
            return JsonSerializer.Deserialize<List<KoblingsLeder>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Skriv(List<KoblingsLeder> ledere) => JsonSerializer.Serialize(ledere);

    // Fyller på med standard farge/funksjon eller kutter, til n ledere.
    public static List<KoblingsLeder> MedAntall(List<KoblingsLeder> ledere, int n, bool par = false)
    {
        n = Math.Clamp(n, 1, MaksLedere);
        var res = ledere.Take(n).ToList();
        while (res.Count < n)
        {
            var i = res.Count;
            res.Add(new KoblingsLeder { Farge = Farger[i].Navn, Funksjon = StandardFunksjon[i] });
        }
        for (var i = 0; i < res.Count; i++)
        {
            res[i].Par = par ? i / 2 + 1 : null;
        }
        return res;
    }

    public static bool ErPar(List<KoblingsLeder> ledere) => ledere.Count > 0 && ledere.All(l => l.Par is not null);

    public static string MalNavn(List<KoblingsLeder> ledere) =>
        ErPar(ledere) ? $"{(ledere.Count + 1) / 2} par" : Type(ledere.Count);

    // "Rød (par 1)" / "Rød stiplet"
    public static string LederTekst(KoblingsLeder l) =>
        l.Farge + (l.Stiplet ? " stiplet" : "") + (l.Par is { } p ? $" (par {p})" : "");

    public static string Type(int antall) => $"{antall}-leder";

    // Punkter går fra kabelen og ut. Indeks/ErStart sier hvilken leder og ende det er.
    // Fri = lederen står løst i vifta (ikke koblet noe sted).
    public sealed record LederLinje(KoblingsLeder Leder, List<KoblingsStrekPunkt> Punkter, int Indeks, bool ErStart, bool Fri = false);

    // Navnet som skrives ved lederen på tegningen: funksjonen, ellers fargen.
    public static string LederNavn(KoblingsLeder l) => string.IsNullOrWhiteSpace(l.Funksjon) ? l.Farge : l.Funksjon;

    // Hvor ledernavnet står: ved enden av lederen, langs den siste biten.
    // Vinkel er i grader på skjermen (alltid lesbar, aldri opp-ned). Etter =
    // teksten står etter punktet i vinkelens retning, ellers foran.
    public sealed record Etikett(KoblingsStrekPunkt Punkt, double Vinkel, bool Etter);

    public static Etikett? LagEtikett(LederLinje l, double aspekt)
    {
        var p = l.Punkter;
        if (p.Count < 2)
        {
            return null;
        }
        var b = p[^1];
        KoblingsStrekPunkt? a = null;
        for (var k = p.Count - 2; k >= 0; k--)
        {
            if (Avstand(p[k], b, aspekt) > 0.01)
            {
                a = p[k];
                break;
            }
        }
        if (a is null)
        {
            return null;
        }
        var v = Math.Atan2((b.Y - a.Y) * aspekt, b.X - a.X) * 180 / Math.PI;
        var etter = l.Fri;
        if (v > 89.5 || v < -90.5)
        {
            v = v > 0 ? v - 180 : v + 180;
            etter = !etter;
        }
        return new Etikett(b, Math.Round(v, 1), etter);
    }

    // Id for en lederende på lerretet (negativ, så den ikke kolliderer med strek-id-er).
    public static int LederId(int strekId, bool erStart, int indeks) => -(strekId * 100 + (erStart ? 0 : 50) + indeks + 1);

    public static (int StrekId, bool ErStart, int Indeks)? LesLederId(int id)
    {
        if (id >= 0)
        {
            return null;
        }
        var v = -id;
        var rest = v % 100;
        var erStart = rest <= 50;
        return (v / 100, erStart, rest - (erStart ? 0 : 50) - 1);
    }

    // Området rundt en kabelende der den tykke kabelen skjules og lederne tar over.
    public sealed record Ende(KoblingsStrekPunkt Senter, double Rx, double Ry);

    public sealed record Vifte(
        List<LederLinje> Ledere,
        List<KoblingsStrekPunkt> Kropp,
        Ende Start,
        Ende Slutt,
        KoblingsStrekPunkt Merke,
        double Mellom);

    // Avstand mellom lederne (KoblingsStrek.LederAvstand) er en faktor lagret som
    // tekst ("0.6"). Null = normal. "Tett"/"Bred" er eldre lagrede verdier.
    public static readonly (double Faktor, string Navn)[] Avstander = [(0.45, "Tett"), (1.0, "Normal"), (1.6, "Bred")];
    public const double MinFaktor = 0.2;
    public const double MaksFaktor = 3.0;

    public static double AvstandFaktor(string? avstand) => avstand switch
    {
        null or "" => 1.0,
        "Tett" => 0.45,
        "Bred" => 1.6,
        _ => double.TryParse(avstand, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) && double.IsFinite(f)
            ? Math.Clamp(f, MinFaktor, MaksFaktor)
            : 1.0
    };

    public static string? AvstandVerdi(double faktor)
    {
        faktor = Math.Round(Math.Clamp(faktor, MinFaktor, MaksFaktor), 2);
        return Math.Abs(faktor - 1.0) < 0.001 ? null : faktor.ToString(CultureInfo.InvariantCulture);
    }

    // Avstanden mellom to naboledere i prosent av lerretets bredde.
    public static double LederAvstand(int antall, string? avstand) =>
        Math.Min(0.75, 7.0 / Math.Max(1, antall)) * AvstandFaktor(avstand);

    // Punktene er i prosent av lerretet. aspekt = høyde/bredde på lerretet,
    // slik at lederne står vinkelrett på kabelen også når lerretet ikke er
    // kvadratisk. Alle punkter som returneres er også i prosent.
    public static Vifte? LagVifte(List<KoblingsStrekPunkt> punkter, List<KoblingsLeder> ledere, double aspekt, string? avstand = null)
    {
        if (punkter.Count < 2 || ledere.Count == 0)
        {
            return null;
        }

        var linjer = new List<LederLinje>();
        var mellom = LederAvstand(ledere.Count, avstand);
        var forste = LagEnde(punkter, ledere, aspekt, mellom, fraStart: true, linjer, out var startKropp, out var startEnde);
        var siste = LagEnde(punkter, ledere, aspekt, mellom, fraStart: false, linjer, out var sluttKropp, out var sluttEnde);
        if (!forste || !siste)
        {
            return null;
        }

        var kropp = new List<KoblingsStrekPunkt> { startKropp };
        kropp.AddRange(punkter.Skip(1).Take(punkter.Count - 2));
        kropp.Add(sluttKropp);

        // Kabelnavnet plasseres midt på den lengste biten.
        var merke = punkter[0];
        var lengst = -1.0;
        for (var i = 0; i < punkter.Count - 1; i++)
        {
            var l = Avstand(punkter[i], punkter[i + 1], aspekt);
            if (l > lengst)
            {
                lengst = l;
                merke = new KoblingsStrekPunkt { X = (punkter[i].X + punkter[i + 1].X) / 2, Y = (punkter[i].Y + punkter[i + 1].Y) / 2 };
            }
        }

        return new Vifte(linjer, kropp, startEnde, sluttEnde, merke, mellom);
    }

    private static bool LagEnde(
        List<KoblingsStrekPunkt> punkter, List<KoblingsLeder> ledere, double aspekt, double mellom, bool fraStart,
        List<LederLinje> linjer, out KoblingsStrekPunkt kroppStart, out Ende ende)
    {
        var p0 = fraStart ? punkter[0] : punkter[^1];
        kroppStart = p0;
        ende = new Ende(p0, 0, 0);

        // Første punkt som faktisk ligger et stykke unna enden.
        KoblingsStrekPunkt? nabo = null;
        var rekke = fraStart ? punkter.Skip(1) : Enumerable.Reverse(punkter).Skip(1);
        foreach (var p in rekke)
        {
            if (Avstand(p0, p, aspekt) > 0.01)
            {
                nabo = p;
                break;
            }
        }
        if (nabo is null)
        {
            return false;
        }

        // Regn i "synlige" enheter (y skalert med aspekt) så vinklene blir riktige.
        // u peker ut fra kabelen (retningen lederne går), n står vinkelrett på den.
        double ax = p0.X, ay = p0.Y * aspekt;
        double bx = nabo.X, by = nabo.Y * aspekt;
        var seg = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
        double ux = -(bx - ax) / seg, uy = -(by - ay) / seg;
        double nx = uy, ny = -ux;

        KoblingsStrekPunkt Til(double cu, double cn) =>
            new() { X = ax + ux * cu + nx * cn, Y = (ay + uy * cu + ny * cn) / aspekt };

        (double U, double N) Fra(KoblingsStrekPunkt p)
        {
            double px = p.X - ax, py = p.Y * aspekt - ay;
            return (px * ux + py * uy, px * nx + py * ny);
        }

        // Den sorte kabelen går helt ut til enden der man klikket, og lederne
        // begynner der og vifter ut videre i samme retning (bort fra kabelen).
        const double vifte = 2.0;
        const double lengde = 4.0;
        const double rett = 0.05;
        var n = ledere.Count;

        // Hver leder får en plass i vifta. Ledere som er koblet til et punkt
        // sorteres etter hvor punktet ligger, så ingen ledere krysser hverandre.
        var plasser = new List<(int I, double Nokkel, (double U, double N)? Mal, KoblingsStrekPunkt? MalPunkt, List<KoblingsStrekPunkt>? Egen)>();
        for (var i = 0; i < n; i++)
        {
            // Speil rekkefølgen i sluttenden så samme leder ligger på samme side av kabelen.
            var o = ((fraStart ? i : n - 1 - i) - (n - 1) / 2.0) * mellom;
            var egen = fraStart ? ledere[i].Start : ledere[i].Slutt;
            var auto = (fraStart ? ledere[i].StartAuto : ledere[i].SluttAuto) && egen is { Count: > 0 };
            var mal = auto ? Fra(egen![^1]) : ((double U, double N)?)null;
            plasser.Add((i, mal?.N ?? o, mal, auto ? egen![^1] : null, auto ? null : egen));
        }
        plasser = plasser.OrderBy(p => p.Nokkel).ToList();

        double Plass(int j) => (j - (n - 1) / 2.0) * mellom;
        bool Bak((double U, double N) m) => m.U < vifte + 1;

        // Ledere som må gå bakover (punktet ligger bak kabelenden) legger seg
        // inni hverandre som en U: da må de bytte plass innbyrdes.
        foreach (var oppover in new[] { true, false })
        {
            var bytt = Enumerable.Range(0, n)
                .Where(j => plasser[j].Mal is { } m && Bak(m) && (oppover ? m.N < Plass(j) - rett : m.N > Plass(j) + rett))
                .ToList();
            var snudd = bytt.Select(j => plasser[j]).Reverse().ToList();
            for (var k = 0; k < bytt.Count; k++)
            {
                plasser[bytt[k]] = snudd[k];
            }
        }

        // Ledere som må svinge til siden svinger i trapp: den ytterste først,
        // så de ikke krysser hverandre på vei inn til punktene sine.
        var trapp = new int[n];
        var opp = 0;
        for (var j = 0; j < n; j++)
        {
            if (plasser[j].Mal is { } m && m.N < Plass(j) - rett)
            {
                trapp[j] = opp++;
            }
        }
        var ned = 0;
        for (var j = n - 1; j >= 0; j--)
        {
            if (plasser[j].Mal is { } m && m.N > Plass(j) + rett)
            {
                trapp[j] = ned++;
            }
        }
        var steg = Math.Max(mellom, 0.6);

        for (var j = 0; j < n; j++)
        {
            var (i, _, mal, malPunkt, egen) = plasser[j];
            var o = Plass(j);
            var fra = new KoblingsStrekPunkt { X = p0.X, Y = p0.Y };
            List<KoblingsStrekPunkt> vei;
            var fri = false;
            if (mal is { } t)
            {
                vei = [fra, Til(vifte, o)];
                if (Math.Abs(t.N - o) >= rett)
                {
                    var c = vifte + steg * (trapp[j] + 1);
                    if (!Bak(t) && c > t.U - 0.4)
                    {
                        c = Math.Max(vifte, t.U - 0.4);
                    }
                    vei.Add(Til(c, o));
                    vei.Add(Til(c, t.N));
                }
                vei.Add(new KoblingsStrekPunkt { X = malPunkt!.X, Y = malPunkt.Y });
            }
            else if (egen is { Count: > 0 })
            {
                vei = [fra, .. egen];
            }
            else
            {
                vei = [fra, Til(vifte, o), Til(lengde, o)];
                fri = true;
            }
            linjer.Add(new LederLinje(ledere[i], UtenDobbelt(vei, aspekt), i, fraStart, fri));
        }

        // Kabelen skal ikke skjules noe sted lenger: masken får radius 0.
        kroppStart = p0;
        ende = new Ende(p0, 0, 0);
        return true;
    }

    private static List<KoblingsStrekPunkt> UtenDobbelt(List<KoblingsStrekPunkt> vei, double aspekt)
    {
        var res = new List<KoblingsStrekPunkt>(vei.Count);
        foreach (var p in vei)
        {
            if (res.Count == 0 || Avstand(res[^1], p, aspekt) > 0.01)
            {
                res.Add(p);
            }
        }
        return res;
    }

    private static double Avstand(KoblingsStrekPunkt a, KoblingsStrekPunkt b, double aspekt)
    {
        var dx = b.X - a.X;
        var dy = (b.Y - a.Y) * aspekt;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
