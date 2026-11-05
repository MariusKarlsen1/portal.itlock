using System.Text.RegularExpressions;

namespace PortalItlock.Web.Services;

// Tolker teksten i «Avvik og anbefalinger» til servicerapporten. Teksten er fritt
// skrevet, ofte med en linje per dør («ID26.227 romnr 229 - Automatikken henger»),
// korte overskrifter («Gammelt Bygg») og en del som heter «Kritisk». Linjer som
// ikke følger mønsteret beholdes som avsnitt, så ingen tekst forsvinner.
public static partial class ServiceAnbefalinger
{
    private const int MaksOverskrift = 30;
    private const int MaksLinjerIFelt = 8;
    private const int MaksTegnIFelt = 900;

    public enum LinjeType { Overskrift, Rad, Avsnitt }

    public sealed record Linje(LinjeType Type, string? Dor, string Tekst);

    public sealed record Tolking(IReadOnlyList<Linje> Linjer, IReadOnlyList<Linje> Kritisk, bool ErLang)
    {
        public int Antall => Linjer.Count(l => l.Type == LinjeType.Rad);

        // Bare overskrifter uten innhold under gir ikke egne sider.
        public bool HarEgneSider => ErLang && Linjer.Any(l => l.Type != LinjeType.Overskrift);
    }

    [GeneratedRegex(@"^(.{1,30}?)\s*-\s+(.+)$")]
    private static partial Regex Rad();

    [GeneratedRegex(@"(^|>)[ \t]*\*[ \t]+", RegexOptions.Multiline)]
    private static partial Regex Stjerne();

    public static Tolking Tolk(string? tekst)
    {
        var alle = RikTekst.Blokker(tekst)
            .Select(b => (b.Type, Tekst: string.Concat(b.Biter.Select(x => x.Tekst)).Trim()))
            .Where(b => b.Tekst.Length > 0)
            .Select(b => TilLinje(b.Type, b.Tekst))
            .ToList();

        var erLang = alle.Count > MaksLinjerIFelt || alle.Sum(l => l.Tekst.Length) > MaksTegnIFelt;
        var kritiskFra = alle.FindIndex(l => l.Type == LinjeType.Overskrift
            && l.Tekst.StartsWith("kritisk", StringComparison.OrdinalIgnoreCase));

        return kritiskFra < 0
            ? new Tolking(alle, [], erLang)
            : new Tolking(alle[..kritiskFra], alle[(kritiskFra + 1)..].Where(l => l.Type != LinjeType.Overskrift).ToList(), erLang);
    }

    // «* Sjekket skruer» skrevet for hånd blir et vanlig punkt.
    public static string MedPunkter(string tekst) => Stjerne().Replace(tekst, "$1• ");

    private static Linje TilLinje(RikTekst.BlokkType type, string tekst)
    {
        if (type is RikTekst.BlokkType.Overskrift or RikTekst.BlokkType.Stor)
        {
            return new Linje(LinjeType.Overskrift, null, tekst.TrimEnd(':'));
        }

        var rad = Rad().Match(tekst);
        if (rad.Success)
        {
            return new Linje(LinjeType.Rad, rad.Groups[1].Value.Trim(), rad.Groups[2].Value.Trim());
        }

        return tekst.Length <= MaksOverskrift && !".?!".Contains(tekst[^1])
            ? new Linje(LinjeType.Overskrift, null, tekst.TrimEnd(':'))
            : new Linje(LinjeType.Avsnitt, null, tekst);
    }
}
