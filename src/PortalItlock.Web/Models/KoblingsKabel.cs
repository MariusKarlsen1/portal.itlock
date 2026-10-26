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
    public sealed record LederLinje(KoblingsLeder Leder, List<KoblingsStrekPunkt> Punkter, int Indeks, bool ErStart);

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
        KoblingsStrekPunkt Merke);

    // Punktene er i prosent av lerretet. aspekt = høyde/bredde på lerretet,
    // slik at lederne står vinkelrett på kabelen også når lerretet ikke er
    // kvadratisk. Alle punkter som returneres er også i prosent.
    public static Vifte? LagVifte(List<KoblingsStrekPunkt> punkter, List<KoblingsLeder> ledere, double aspekt)
    {
        if (punkter.Count < 2 || ledere.Count == 0)
        {
            return null;
        }

        var linjer = new List<LederLinje>();
        var forste = LagEnde(punkter, ledere, aspekt, fraStart: true, linjer, out var startKropp, out var startEnde);
        var siste = LagEnde(punkter, ledere, aspekt, fraStart: false, linjer, out var sluttKropp, out var sluttEnde);
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

        return new Vifte(linjer, kropp, startEnde, sluttEnde, merke);
    }

    private static bool LagEnde(
        List<KoblingsStrekPunkt> punkter, List<KoblingsLeder> ledere, double aspekt, bool fraStart,
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
        double ax = p0.X, ay = p0.Y * aspekt;
        double bx = nabo.X, by = nabo.Y * aspekt;
        var seg = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
        double dx = (bx - ax) / seg, dy = (by - ay) / seg;
        double nx = -dy, ny = dx;

        var maksAndel = punkter.Count == 2 ? 0.3 : 0.45;
        var lengde = Math.Min(4.0, seg * maksAndel);
        var n = ledere.Count;
        var avstand = Math.Min(0.75, 7.0 / n);
        var cx = ax + dx * lengde;
        var cy = ay + dy * lengde;

        for (var i = 0; i < n; i++)
        {
            // Speil rekkefølgen i sluttenden så samme leder ligger på samme side av kabelen.
            var o = ((fraStart ? i : n - 1 - i) - (n - 1) / 2.0) * avstand;
            var x1 = ax + nx * o;
            var y1 = ay + ny * o;
            var x2 = x1 + dx * lengde * 0.4;
            var y2 = y1 + dy * lengde * 0.4;
            var fra = new KoblingsStrekPunkt { X = cx, Y = cy / aspekt };
            var egen = fraStart ? ledere[i].Start : ledere[i].Slutt;
            List<KoblingsStrekPunkt> vei = egen is { Count: > 0 }
                ? [fra, .. egen]
                : [fra, new KoblingsStrekPunkt { X = x2, Y = y2 / aspekt }, new KoblingsStrekPunkt { X = x1, Y = y1 / aspekt }];
            linjer.Add(new LederLinje(ledere[i], vei, i, fraStart));
        }

        kroppStart = new KoblingsStrekPunkt { X = cx, Y = cy / aspekt };
        var r = lengde * 0.97;
        ende = new Ende(p0, r, r / aspekt);
        return true;
    }

    private static double Avstand(KoblingsStrekPunkt a, KoblingsStrekPunkt b, double aspekt)
    {
        var dx = b.X - a.X;
        var dy = (b.Y - a.Y) * aspekt;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
