using System.Text.RegularExpressions;

namespace PortalItlock.Web.Services;

// Leser dørtypeskjemaer fra arkitekt (ArchiCAD-oppsett: «Skjema innerdører/ytterdører»)
// der hver kolonne er én dørtype: ID, Slagretning, Åpningstype, Antall, Størrelse B=/H=,
// Veggåpningsmål, Brannkrav, Lydkrav, U-verdi, Funksjon, Terskel, Glass, Glassareal,
// Dørblad og Anmerkning. Samme ID kan stå i flere kolonner (ulik slagretning/anmerkning);
// de slås sammen til én Dør ID.
public static partial class DorTypeSkjemaLeser
{
    public sealed record Kolonne(string Kode, string? Slag, int Antall, string? Apningstype, int? Bredde, int? Hoyde, bool ToFloyet,
        string? Brann, string? Lyd, string? UVerdi, string? Funksjon, string? Terskel, string? Glass, decimal? Glassareal, string? Dorblad, string? Anmerkning);

    public sealed record DorType(string Kode, int AntallH, int AntallV, int AntallUten, string? Apningstype, int? Bredde, int? Hoyde, bool ToFloyet,
        string? Brann, string? Lyd, string? UVerdi, string? Funksjon, string? Terskel, string? Glass, decimal? Glassareal, string? Dorblad, List<string> Anmerkninger)
    {
        public int Antall => AntallH + AntallV + AntallUten;
        public bool FriBredde860 => Anmerkninger.Any(a => a.Contains("fri bredde", StringComparison.OrdinalIgnoreCase));
        public bool Romningsvei => Anmerkninger.Any(a => a.Contains("rømningsvei", StringComparison.OrdinalIgnoreCase));
    }

    private static readonly string[] Etiketter =
        ["ID", "Slagretning", "Åpningstype", "Antall", "Sett fra hengslesiden", "Størrelse", "B=", "H=", "Veggåpningsmål",
         "Brannkrav", "Lydkrav", "U-verdi", "Funksjon", "Terskel", "Glass", "Glassareal", "Dørblad", "Anmerkning"];

    private const string Sett = "Sett fra hengslesiden";

    [GeneratedRegex(@"^[A-ZÆØÅ]{1,4}[-_]?\d[\wÆØÅæøå-]*$")]
    private static partial Regex KodeRegex();

    [GeneratedRegex(@"\d{1,3}(?:[    ]\d{3})*")]
    private static partial Regex TallRegex();

    [GeneratedRegex(@"^\s*([HV])\b", RegexOptions.IgnoreCase)]
    private static partial Regex SlagRegex();

    private sealed record Linje(double Y, string Tekst, double X0);

    private static List<Linje> Linjer(IEnumerable<PdfTekst.Ord> ord, double tol = 2.5)
    {
        var grupper = new List<(double Y, List<PdfTekst.Ord> Ord)>();
        foreach (var w in ord.OrderBy(o => o.MidtY))
        {
            var g = grupper.FindIndex(x => Math.Abs(x.Y - w.MidtY) < tol);
            if (g >= 0)
            {
                grupper[g].Ord.Add(w);
            }
            else
            {
                grupper.Add((w.MidtY, [w]));
            }
        }
        return grupper.Select(g => new Linje(g.Y, string.Join(' ', g.Ord.OrderBy(o => o.X0).Select(o => o.Tekst)), g.Ord.Min(o => o.X0))).ToList();
    }

    private static double Median(List<double> v)
    {
        if (v.Count == 0) return 0;
        var s = v.Order().ToList();
        return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
    }

    public static int? Tall(string? s)
    {
        var m = TallRegex().Match(s ?? "");
        return m.Success && int.TryParse(Regex.Replace(m.Value, @"\D", ""), out var n) ? n : null;
    }

    private static List<int> AlleTall(string s) =>
        TallRegex().Matches(s).Select(m => int.TryParse(Regex.Replace(m.Value, @"\D", ""), out var n) ? n : 0).ToList();

    public static List<Kolonne> LesKolonner(List<PdfTekst.Side> sider)
    {
        var ut = new List<Kolonne>();
        foreach (var side in sider)
        {
            var ord = side.Ord;
            var idRader = new List<(PdfTekst.Ord Etikett, List<PdfTekst.Ord> Koder)>();
            foreach (var w in ord.Where(o => o.Tekst == "ID"))
            {
                var koder = ord.Where(v => Math.Abs(v.MidtY - w.MidtY) < 2 && v.X0 > w.X1 + 5 && KodeRegex().IsMatch(v.Tekst))
                    .OrderBy(v => v.X0).ToList();
                if (koder.Count > 0)
                {
                    idRader.Add((w, koder));
                }
            }

            foreach (var (idw, koder) in idRader)
            {
                var venstre = idw.X0;
                var topp = idw.Y0;
                var under = idRader.Where(r => r.Etikett.Y0 > topp + 5 && Math.Abs(r.Etikett.X0 - venstre) < 30).Select(r => r.Etikett.Y0).ToList();
                var bunn = under.Count > 0 ? under.Min() - 2 : side.Hoyde;
                var starter = koder.Select(k => k.X0).ToList();
                var bredde = starter.Count > 1 ? Median(starter.Zip(starter.Skip(1), (a, b) => b - a).ToList()) : 200;
                var labelGrense = starter[0] - 2;

                var etiketter = new List<(double Y, string Navn)>();
                foreach (var l in Linjer(ord.Where(w => w.X0 >= venstre - 3 && w.X0 < labelGrense && w.Y0 >= topp - 2 && w.Y0 < bunn)))
                {
                    var t = l.Tekst.Trim();
                    var treff = Etiketter.FirstOrDefault(e => t.StartsWith(e, StringComparison.Ordinal) && etiketter.All(x => x.Navn != e));
                    if (treff is not null)
                    {
                        etiketter.Add((l.Y, treff));
                        var rest = t[treff.Length..].Trim();
                        if (rest is "B=" or "H=")
                        {
                            etiketter.Add((l.Y, rest));
                        }
                    }
                    else if (t is "B=" or "H=" && etiketter.All(x => x.Navn != t))
                    {
                        etiketter.Add((l.Y, t));
                    }
                }
                etiketter = etiketter.OrderBy(e => e.Y).ToList();
                if (etiketter.Count < 4)
                {
                    continue;
                }
                var avst = etiketter.Zip(etiketter.Skip(1), (a, b) => b.Y - a.Y).Where(d => d > 0.5).ToList();
                var radh = avst.Count > 0 ? Median(avst) / 2 : 6;
                var siste = etiketter[^1].Y;
                var harSett = etiketter.Any(e => e.Navn == Sett);

                for (var i = 0; i < koder.Count; i++)
                {
                    var x0 = starter[i] - 2;
                    var x1 = i + 1 < starter.Count ? starter[i + 1] - 2 : starter[i] + bredde - 2;
                    var celle = ord.Where(w => w.X0 >= x0 && w.X0 < x1 && w.Y0 >= topp - 2 && w.Y1 <= siste + radh * 3);
                    var rad = etiketter.Select(e => e.Navn).Distinct().ToDictionary(e => e, _ => new List<string>());
                    foreach (var l in Linjer(celle))
                    {
                        var nest = etiketter.MinBy(e => Math.Abs(e.Y - l.Y));
                        if (nest.Navn != Sett && Math.Abs(nest.Y - l.Y) > radh * 2.2)
                        {
                            if (!harSett) continue;
                            nest = etiketter.First(e => e.Navn == Sett);
                        }
                        rad[nest.Navn].Add(l.Tekst);
                    }

                    string? Tekst(string e) => rad.TryGetValue(e, out var v) && v.Count > 0 ? string.Join(' ', v).Trim() : null;
                    string? Verdi(string e) { var t = Tekst(e); return string.IsNullOrWhiteSpace(t) || t == "-" ? null : t; }

                    var kode = koder[i].Tekst;
                    var slagTekst = Tekst("Slagretning") ?? "";
                    var slagM = SlagRegex().Match(slagTekst);
                    var antall = Tall(Tekst("Antall")) ?? Tall(Regex.Match(slagTekst, @"\((\d+)\)").Groups[1].Value) ?? 1;
                    var b = Tall(Tekst("B="));
                    var h = Tall(Tekst("H="));
                    var apning = Verdi("Åpningstype");
                    var talliBilde = AlleTall(string.Join(' ', (rad.GetValueOrDefault(Sett) ?? []).Concat(rad.GetValueOrDefault("B=") ?? []))).Where(n => n >= 250).ToList();
                    var toFloyet = (apning?.Contains("dobbel", StringComparison.OrdinalIgnoreCase) ?? false)
                        || (b is int bb && talliBilde.Where((a, ai) => talliBilde.Skip(ai + 1).Any(c => Math.Abs(a + c - bb) <= 5)).Any());
                    decimal? glassareal = decimal.TryParse((Verdi("Glassareal") ?? "").Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var ga) ? ga : null;

                    ut.Add(new Kolonne(kode, slagM.Success ? slagM.Groups[1].Value.ToUpperInvariant() : null, antall, apning, b, h, toFloyet,
                        Verdi("Brannkrav"), Verdi("Lydkrav"), Verdi("U-verdi"), Verdi("Funksjon"), Verdi("Terskel"), Verdi("Glass"), glassareal,
                        Verdi("Dørblad"), Verdi("Anmerkning")));
                }
            }
        }
        return ut;
    }

    public static List<DorType> SlaSammen(IEnumerable<Kolonne> kolonner) => kolonner
        .GroupBy(k => k.Kode, StringComparer.OrdinalIgnoreCase)
        .Select(g =>
        {
            string? Forste(Func<Kolonne, string?> f) => g.Select(f).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            string? Alle(Func<Kolonne, string?> f)
            {
                var v = g.Select(f).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct().ToList();
                return v.Count == 0 ? null : string.Join("; ", v);
            }
            return new DorType(g.First().Kode,
                g.Where(k => k.Slag == "H").Sum(k => k.Antall),
                g.Where(k => k.Slag == "V").Sum(k => k.Antall),
                g.Where(k => k.Slag is null).Sum(k => k.Antall),
                Forste(k => k.Apningstype),
                g.Select(k => k.Bredde).FirstOrDefault(v => v is not null),
                g.Select(k => k.Hoyde).FirstOrDefault(v => v is not null),
                g.Any(k => k.ToFloyet),
                Forste(k => k.Brann), Forste(k => k.Lyd), Forste(k => k.UVerdi), Alle(k => k.Funksjon), Forste(k => k.Terskel),
                Forste(k => k.Glass), g.Select(k => k.Glassareal).FirstOrDefault(v => v is > 0), Forste(k => k.Dorblad),
                g.Select(k => k.Anmerkning).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a!.Trim()).Distinct().ToList());
        })
        .OrderBy(t => t.Kode, NaturligSammenligner.Instans)
        .ToList();

    // Samme Dør ID selv om den er skrevet litt ulikt («ID21», «ID-21», «id 21»).
    public static string Norm(string? kode) => Regex.Replace((kode ?? "").ToUpperInvariant(), "[^A-Z0-9ÆØÅ]", "");

    // Dørtype ut fra prefikset i ID-en (forklaringen står i skjemaet: ID-, BD-, IDS-, VSD-, YD-, YDP-).
    public static string? Konstruksjon(string kode)
    {
        var prefiks = kode.Split('-')[0].ToUpperInvariant();
        return prefiks switch
        {
            "ID" => "Innerdør",
            "BD" => "Boddør",
            "IDS" => "Innvendig skyvedør",
            "VSD" => "Skyvedør i yttervegg",
            "YD" => "Ytterdør",
            "YDP" => "Port",
            _ => null
        };
    }
}
