using System.Text.RegularExpressions;

namespace PortalItlock.Web.Services;

// Sorterer tekst med tall slik folk forventer: D-2 før D-10, 1. etasje før 10. etasje.
public sealed partial class NaturligSammenligner : IComparer<string?>
{
    public static readonly NaturligSammenligner Instans = new();

    [GeneratedRegex(@"\d+|\D+")]
    private static partial Regex Biter();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        var a = Biter().Matches(x);
        var b = Biter().Matches(y);
        for (var i = 0; i < Math.Min(a.Count, b.Count); i++)
        {
            var sa = a[i].Value;
            var sb = b[i].Value;
            int c;
            if (char.IsDigit(sa[0]) && char.IsDigit(sb[0]))
            {
                c = sa.TrimStart('0').Length.CompareTo(sb.TrimStart('0').Length);
                if (c == 0) c = string.CompareOrdinal(sa.TrimStart('0'), sb.TrimStart('0'));
            }
            else
            {
                c = string.Compare(sa, sb, StringComparison.CurrentCultureIgnoreCase);
            }
            if (c != 0) return c;
        }
        return a.Count.CompareTo(b.Count);
    }
}
