using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Opsjonstilbud per dør: rekkefølge, gruppering og overskrift på dørene.
// Brukes både av velgeren på tilbudet, når varelinjene synkes og i PDF-en,
// så alle tre viser dørene i samme rekkefølge.
//
// Lagres på tilbudet som kommalister:
//   OpsjonSortering  "etasje,id" eller "-navn" (minus = synkende). Tom = dør-ID.
//   OpsjonGruppering "etasje,pakke". Tom = ingen gruppering.
//   OpsjonOverskrift "id,navn,etasje,pakke". Tom = dør-ID og dørnavn.
public static class OpsjonDorOrden
{
    public sealed record DorInfo(int Id, string Dornummer, string? Navn, string? Etasje, string? Pakke);

    public static readonly IReadOnlyDictionary<string, string> Felt = new Dictionary<string, string>
    {
        ["id"] = "Dør-ID",
        ["navn"] = "Dørnavn",
        ["etasje"] = "Etasje",
        ["pakke"] = "Pakke",
    };

    public static readonly IReadOnlyDictionary<string, string> GruppeFelt = new Dictionary<string, string>
    {
        ["etasje"] = "Etasje",
        ["pakke"] = "Pakke",
    };

    public static string UtenVerdi(string felt) => felt == "pakke" ? "Uten pakke" : "Uten etasje";

    public static DorInfo Info(Dor d, string? pakke) =>
        new(d.Id, d.Dornummer, Tom(d.DorTil), Tom(d.Etasje), Tom(pakke));

    public static string? Verdi(DorInfo d, string felt) => felt switch
    {
        "id" => d.Dornummer,
        "navn" => d.Navn,
        "etasje" => d.Etasje,
        "pakke" => d.Pakke,
        _ => null
    };

    public static string GruppeVerdi(DorInfo d, string felt) => Verdi(d, felt) ?? UtenVerdi(felt);

    // (felt, stigende) – ukjente felt hoppes over.
    public static List<(string Felt, bool Stigende)> LesSortering(string? s)
    {
        var liste = Liste(s)
            .Select(x => x.StartsWith('-') ? (x[1..], false) : (x, true))
            .Where(x => Felt.ContainsKey(x.Item1))
            .ToList();
        return liste.Count == 0 ? [("id", true)] : liste;
    }

    public static string LagSortering(IEnumerable<(string Felt, bool Stigende)> s) =>
        string.Join(",", s.Select(x => x.Stigende ? x.Felt : "-" + x.Felt));

    public static List<string> LesGruppering(string? s) =>
        Liste(s).Where(GruppeFelt.ContainsKey).Distinct().Take(2).ToList();

    public static List<string> LesOverskrift(string? s)
    {
        var l = Liste(s).Where(Felt.ContainsKey).Distinct().ToList();
        return l.Count == 0 ? ["id", "navn"] : l;
    }

    // Overskriften til døren på tilbudet, f.eks. «D-101 Hovedinngang · 1. etg».
    public static string Overskrift(DorInfo d, string? felt)
    {
        var deler = LesOverskrift(felt).Select(f => Verdi(d, f)).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (deler.Count == 0)
        {
            return d.Dornummer;
        }
        // Dør-ID og dørnavn står sammen som før («D-101 Hovedinngang»), resten skilles med ·.
        var felt2 = LesOverskrift(felt);
        if (felt2.Count >= 2 && felt2[0] == "id" && felt2[1] == "navn" && d.Navn is not null)
        {
            return string.Join(" · ", new[] { $"{d.Dornummer} {d.Navn}" }
                .Concat(felt2.Skip(2).Select(f => Verdi(d, f)).Where(v => !string.IsNullOrWhiteSpace(v)))!);
        }
        return string.Join(" · ", deler);
    }

    private static readonly NaturligSammenligner Naturlig = new();

    // «D-9» før «D-10»: tallene sammenlignes som tall.
    private sealed class NaturligSammenligner : IComparer<string>
    {
        private static readonly System.Globalization.CompareInfo Norsk = new System.Globalization.CultureInfo("nb-NO").CompareInfo;

        public int Compare(string? x, string? y)
        {
            x ??= ""; y ??= "";
            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
                {
                    var si = i; while (i < x.Length && char.IsDigit(x[i])) i++;
                    var sj = j; while (j < y.Length && char.IsDigit(y[j])) j++;
                    var a = x[si..i].TrimStart('0'); var b = y[sj..j].TrimStart('0');
                    var r = a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
                    if (r != 0) return r;
                }
                else
                {
                    var si = i; while (i < x.Length && !char.IsDigit(x[i])) i++;
                    var sj = j; while (j < y.Length && !char.IsDigit(y[j])) j++;
                    var r = Norsk.Compare(x[si..i], y[sj..j], System.Globalization.CompareOptions.IgnoreCase);
                    if (r != 0) return r;
                }
            }
            return (x.Length - i).CompareTo(y.Length - j);
        }
    }

    private static int Sammenlign(string? a, string? b)
    {
        // Tomme verdier står sist.
        if (a is null || b is null)
        {
            return (a is null).CompareTo(b is null);
        }
        return Naturlig.Compare(a, b);
    }

    private static int SammenlignGruppe(string a, string b, string felt)
    {
        var ua = a == UtenVerdi(felt);
        var ub = b == UtenVerdi(felt);
        return ua != ub ? ua.CompareTo(ub) : Naturlig.Compare(a, b);
    }

    // Dørene i den rekkefølgen de skal stå på tilbudet: først gruppene, så sorteringen.
    public static List<DorInfo> Ordne(IEnumerable<DorInfo> dorer, string? sortering, string? gruppering)
    {
        var sort = LesSortering(sortering);
        var grupper = LesGruppering(gruppering);
        var l = dorer.ToList();
        l.Sort((a, b) =>
        {
            foreach (var g in grupper)
            {
                var r = SammenlignGruppe(GruppeVerdi(a, g), GruppeVerdi(b, g), g);
                if (r != 0)
                {
                    return r;
                }
            }
            foreach (var (felt, stigende) in sort)
            {
                var r = Sammenlign(Verdi(a, felt), Verdi(b, felt));
                if (r != 0)
                {
                    return stigende ? r : -r;
                }
            }
            return Naturlig.Compare(a.Dornummer, b.Dornummer);
        });
        return l;
    }

    private static string? Tom(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static IEnumerable<string> Liste(string? s) =>
        (s ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
