using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PortalItlock.Web.Services;

// Formatert tekst i sluttrapporten (Hva er gjort / Avvik og anbefalinger).
// Lagres som et lite, renset HTML-utvalg: avsnitt, stor tekst (h4),
// overskrift (h3), fet, kursiv, understrek og lister. Eldre ren tekst
// vises som før (linjeskift blir avsnitt).
public static partial class RikTekst
{
    public enum BlokkType { Avsnitt, Stor, Overskrift, Punkt, Nummer }

    public sealed record Bit(string Tekst, bool Fet, bool Kursiv, bool Understrek);

    public sealed record Blokk(BlokkType Type, List<Bit> Biter, int Nummer = 0);

    [GeneratedRegex(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)[^>]*>|([^<]+)", RegexOptions.Singleline)]
    private static partial Regex Token();

    public static bool ErFormatert(string? s) =>
        !string.IsNullOrWhiteSpace(s) && s.TrimStart().StartsWith('<');

    // Deler opp teksten i blokker med tekstbiter. Ukjente tagger hoppes over,
    // teksten i dem beholdes.
    public static List<Blokk> Blokker(string? s)
    {
        var blokker = new List<Blokk>();
        if (string.IsNullOrWhiteSpace(s))
        {
            return blokker;
        }

        if (!ErFormatert(s))
        {
            foreach (var linje in s.Replace("\r", "").Split('\n'))
            {
                blokker.Add(new Blokk(BlokkType.Avsnitt, [new Bit(linje, false, false, false)]));
            }

            return Trim(blokker);
        }

        int fet = 0, kursiv = 0, understrek = 0;
        var lister = new Stack<(bool Nummerert, int Teller)>();
        var type = BlokkType.Avsnitt;
        var nummer = 0;
        var biter = new List<Bit>();

        void Avslutt()
        {
            if (biter.Count > 0)
            {
                blokker.Add(new Blokk(type, biter, nummer));
            }

            biter = [];
            type = lister.Count > 0 ? (lister.Peek().Nummerert ? BlokkType.Nummer : BlokkType.Punkt) : BlokkType.Avsnitt;
        }

        foreach (Match m in Token().Matches(s))
        {
            if (m.Groups[3].Success)
            {
                var tekst = WebUtility.HtmlDecode(m.Groups[3].Value).Replace("\r", "").Replace("\n", " ");
                if (tekst.Length > 0)
                {
                    biter.Add(new Bit(tekst, fet > 0, kursiv > 0, understrek > 0));
                }

                continue;
            }

            var slutt = m.Groups[1].Value == "/";
            switch (m.Groups[2].Value.ToLowerInvariant())
            {
                case "b" or "strong": fet = Math.Max(0, fet + (slutt ? -1 : 1)); break;
                case "i" or "em": kursiv = Math.Max(0, kursiv + (slutt ? -1 : 1)); break;
                case "u": understrek = Math.Max(0, understrek + (slutt ? -1 : 1)); break;
                case "br": Avslutt(); break;
                case "p" or "div":
                    Avslutt();
                    break;
                case "h1" or "h2" or "h3":
                    Avslutt();
                    if (!slutt) type = BlokkType.Overskrift;
                    break;
                case "h4" or "h5" or "h6":
                    Avslutt();
                    if (!slutt) type = BlokkType.Stor;
                    break;
                case "ul" or "ol":
                    Avslutt();
                    if (slutt) { if (lister.Count > 0) lister.Pop(); }
                    else lister.Push((m.Groups[2].Value.Equals("ol", StringComparison.OrdinalIgnoreCase), 0));
                    type = lister.Count > 0 ? (lister.Peek().Nummerert ? BlokkType.Nummer : BlokkType.Punkt) : BlokkType.Avsnitt;
                    break;
                case "li":
                    Avslutt();
                    if (!slutt && lister.Count > 0)
                    {
                        var (n, t) = lister.Pop();
                        lister.Push((n, t + 1));
                        type = n ? BlokkType.Nummer : BlokkType.Punkt;
                        nummer = t + 1;
                    }
                    break;
            }
        }

        Avslutt();
        return Trim(blokker);
    }

    // Fjerner tomme blokker i starten og slutten (men beholder tomme linjer inni).
    private static List<Blokk> Trim(List<Blokk> blokker)
    {
        static bool Tom(Blokk b) => b.Biter.All(x => string.IsNullOrWhiteSpace(x.Tekst));
        while (blokker.Count > 0 && Tom(blokker[0])) blokker.RemoveAt(0);
        while (blokker.Count > 0 && Tom(blokker[^1])) blokker.RemoveAt(blokker.Count - 1);
        return blokker;
    }

    // Renset HTML: bare taggene vi støtter, all tekst kodet.
    public static string TilHtml(string? s)
    {
        var sb = new StringBuilder();
        string? aapenListe = null;
        foreach (var b in Blokker(s))
        {
            var liste = b.Type switch { BlokkType.Punkt => "ul", BlokkType.Nummer => "ol", _ => null };
            if (liste != aapenListe)
            {
                if (aapenListe is not null) sb.Append("</").Append(aapenListe).Append('>');
                if (liste is not null) sb.Append('<').Append(liste).Append('>');
                aapenListe = liste;
            }

            var tag = b.Type switch { BlokkType.Overskrift => "h3", BlokkType.Stor => "h4", BlokkType.Avsnitt => "p", _ => "li" };
            sb.Append('<').Append(tag).Append('>');
            if (b.Biter.All(x => x.Tekst.Length == 0))
            {
                sb.Append("<br>");
            }

            foreach (var bit in b.Biter)
            {
                var t = Kod(bit.Tekst);
                if (bit.Understrek) t = $"<u>{t}</u>";
                if (bit.Kursiv) t = $"<i>{t}</i>";
                if (bit.Fet) t = $"<b>{t}</b>";
                sb.Append(t);
            }

            sb.Append("</").Append(tag).Append('>');
        }

        if (aapenListe is not null) sb.Append("</").Append(aapenListe).Append('>');
        return sb.ToString();
    }

    // Koder bare det som må kodes, så æøå lagres som vanlige bokstaver.
    private static string Kod(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // Lagres i databasen: renset HTML, eller null når feltet er tomt.
    public static string? Rens(string? html)
    {
        var renset = TilHtml(html);
        return Blokker(html).Count == 0 ? null : renset;
    }

    // Ren tekst (lister med «• » og «1. »), til korte sammendrag og notater.
    public static string TilTekst(string? s) =>
        string.Join("\n", Blokker(s).Select(b =>
            (b.Type switch { BlokkType.Punkt => "• ", BlokkType.Nummer => $"{b.Nummer}. ", _ => "" })
            + string.Concat(b.Biter.Select(x => x.Tekst))));
}
