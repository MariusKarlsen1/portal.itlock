using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using IElement = AngleSharp.Dom.IElement;
using INode = AngleSharp.Dom.INode;
using INodeList = AngleSharp.Dom.INodeList;
using IText = AngleSharp.Dom.IText;

namespace PortalItlock.Web.Services;

/// <summary>
/// Rendrer forside-/forbeholdstekst (HTML fra den innebygde WYSIWYG-editoren) til QuestPDF.
/// Delt mellom tilbuds-PDFen, FDV-PDFen og servicerapporten slik at alle kan bruke samme
/// forside-maler. Tar med formateringen fra editoren: overskrifter, skriftstørrelse,
/// fet/kursiv/understrek/gjennomstreking, tekstfarge, justering, linjeavstand og
/// punkt-/nummererte lister (også lister i lister).
/// </summary>
public static class ForsideRenderer
{
    private const float StandardLinjeavstand = 1.35f;

    // Formatering som arves nedover i en tekstblokk.
    private readonly record struct Stil(bool Fet, bool Kursiv, bool Understrek, bool Gjennomstrek, float? Storrelse, string? Farge);

    public static void Render(ColumnDescriptor col, string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return;
        }

        var parser = new HtmlParser();
        using var document = parser.ParseDocument($"<!doctype html><body>{html}</body>");
        RenderBlocks(col, document.Body!.ChildNodes);
    }

    // Kun http(s)/mailto - uten denne sjekken kunne en lagret
    // "javascript:"/"data:"-lenke endt opp som en klikkbar hyperlenke i
    // PDF-en (se sikkerhetsgjennomgangen 2026-10-08). Egen metode (i stedet
    // for inline i RenderNode) slik at den kan regresjonstestes isolert.
    internal static bool ErTryggHyperlenke(string? href) =>
        !string.IsNullOrWhiteSpace(href) && (
            href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));

    private static void RenderBlocks(ColumnDescriptor col, INodeList nodes)
    {
        foreach (var node in nodes)
        {
            if (node is IText textNode)
            {
                if (!string.IsNullOrWhiteSpace(textNode.TextContent))
                {
                    col.Item().PaddingBottom(4).Text(t =>
                    {
                        t.DefaultTextStyle(x => x.LineHeight(StandardLinjeavstand));
                        t.Span(NormalizeWhitespace(textNode.TextContent));
                    });
                }

                continue;
            }

            if (node is not IElement el)
            {
                continue;
            }

            switch (el.TagName.ToLowerInvariant())
            {
                case "h1":
                    Tekstblokk(col, el, 8, 2, new Stil(true, false, false, false, 19, null));
                    break;

                case "h2":
                    Tekstblokk(col, el, 6, 2, new Stil(true, false, false, false, 14, null));
                    break;

                case "h3":
                    Tekstblokk(col, el, 4, 2, new Stil(true, false, false, false, 11, null));
                    break;

                case "ul":
                    RenderList(col, el, ordered: false, niva: 0);
                    break;

                case "ol":
                    RenderList(col, el, ordered: true, niva: 0);
                    break;

                case "br":
                    break;

                case "div" when el.Children.Any(ErBlokk):
                    // En <div> med avsnitt/lister inni - gå inn i den.
                    RenderBlocks(col, el.ChildNodes);
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(el.TextContent))
                    {
                        col.Item().Height(6);
                        break;
                    }

                    Tekstblokk(col, el, 0, 4, default);
                    break;
            }
        }
    }

    private static bool ErBlokk(IElement el) =>
        el.TagName.ToLowerInvariant() is "p" or "div" or "h1" or "h2" or "h3" or "ul" or "ol";

    // Ett avsnitt / én overskrift med justering, linjeavstand og formatering.
    private static void Tekstblokk(ColumnDescriptor col, IElement el, float over, float under, Stil grunnstil)
    {
        var stil = LesInlineStil(el, grunnstil);
        var linjeavstand = LesLinjeavstand(el) ?? StandardLinjeavstand;
        var justering = LesJustering(el);

        var item = col.Item();
        if (over > 0) { item = item.PaddingTop(over); }
        if (under > 0) { item = item.PaddingBottom(under); }

        item.Text(t =>
        {
            t.DefaultTextStyle(x => x.LineHeight(linjeavstand));
            Juster(t, justering);
            foreach (var child in el.ChildNodes)
            {
                RenderInlineNode(t, child, stil);
            }
        });
    }

    private static void RenderList(ColumnDescriptor col, IElement listEl, bool ordered, int niva)
    {
        var container = col.Item();
        if (niva == 0) { container = container.PaddingBottom(4); }
        else { container = container.PaddingLeft(14); }

        var listeLinjeavstand = LesLinjeavstand(listEl);

        container.Column(listCol =>
        {
            var index = 1;
            foreach (var child in listEl.Children)
            {
                if (!string.Equals(child.TagName, "li", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var marker = ordered ? $"{index}." : niva % 2 == 0 ? "•" : "◦";
                index++;

                var stil = LesInlineStil(child, default);
                var linjeavstand = LesLinjeavstand(child) ?? listeLinjeavstand ?? 1.3f;
                var underlister = child.Children.Where(c => c.TagName.ToLowerInvariant() is "ul" or "ol").ToList();

                listCol.Item().PaddingBottom(2).Row(row =>
                {
                    row.ConstantItem(16).Text(t =>
                    {
                        var span = t.Span(marker);
                        if (stil.Storrelse is { } s) { span.FontSize(s); }
                        if (stil.Farge is { } f) { span.FontColor(f); }
                    });
                    row.RelativeItem().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.LineHeight(linjeavstand));
                        Juster(t, LesJustering(child));
                        foreach (var node in child.ChildNodes)
                        {
                            if (node is IElement e && e.TagName.ToLowerInvariant() is "ul" or "ol")
                            {
                                continue;
                            }
                            RenderInlineNode(t, node, stil);
                        }
                    });
                });

                foreach (var under in underlister)
                {
                    RenderList(listCol, under, under.TagName.Equals("ol", StringComparison.OrdinalIgnoreCase), niva + 1);
                }
            }
        });
    }

    private static void RenderInlineNode(TextDescriptor t, INode node, Stil stil)
    {
        if (node is IText text)
        {
            var value = NormalizeWhitespace(text.TextContent);
            if (value.Length == 0)
            {
                return;
            }

            var span = t.Span(value);
            if (stil.Fet) { span.Bold(); }
            if (stil.Kursiv) { span.Italic(); }
            if (stil.Understrek) { span.Underline(); }
            if (stil.Gjennomstrek) { span.Strikethrough(); }
            if (stil.Storrelse is { } storrelse) { span.FontSize(storrelse); }
            if (stil.Farge is { } farge) { span.FontColor(farge); }
            return;
        }

        if (node is not IElement el)
        {
            return;
        }

        switch (el.TagName.ToLowerInvariant())
        {
            case "br":
                t.EmptyLine();
                return;

            case "a":
                var href = el.GetAttribute("href");
                if (ErTryggHyperlenke(href))
                {
                    var lenke = t.Hyperlink(el.TextContent, href!);
                    if (stil.Storrelse is { } s) { lenke.FontSize(s); }
                    return;
                }

                break;
        }

        var nyStil = LesInlineStil(el, stil);
        foreach (var child in el.ChildNodes)
        {
            RenderInlineNode(t, child, nyStil);
        }
    }

    // Leser formatering fra taggen (b/i/u/s/font) og inline style
    // (font-size, font-weight, font-style, text-decoration, color).
    private static Stil LesInlineStil(IElement el, Stil arvet)
    {
        var stil = arvet;
        switch (el.TagName.ToLowerInvariant())
        {
            case "strong":
            case "b":
                stil = stil with { Fet = true };
                break;
            case "em":
            case "i":
                stil = stil with { Kursiv = true };
                break;
            case "u":
                stil = stil with { Understrek = true };
                break;
            case "s":
            case "strike":
            case "del":
                stil = stil with { Gjennomstrek = true };
                break;
            case "font":
                if (TilFarge(el.GetAttribute("color")) is { } fontFarge) { stil = stil with { Farge = fontFarge }; }
                if (int.TryParse(el.GetAttribute("size"), out var htmlStorrelse))
                {
                    stil = stil with { Storrelse = htmlStorrelse switch { <= 1 => 7.5f, 2 => 9, 3 => 10, 4 => 12, 5 => 15, 6 => 20, _ => 26 } };
                }
                break;
        }

        var css = LesCss(el);
        if (css.TryGetValue("font-size", out var fs) && TilPunkter(fs) is { } punkter)
        {
            stil = stil with { Storrelse = punkter };
        }
        if (css.TryGetValue("font-weight", out var fw))
        {
            stil = stil with { Fet = fw is "bold" or "bolder" || (int.TryParse(fw, out var vekt) && vekt >= 600) };
        }
        if (css.TryGetValue("font-style", out var fst))
        {
            stil = stil with { Kursiv = fst is "italic" or "oblique" };
        }
        if (css.TryGetValue("text-decoration", out var td) || css.TryGetValue("text-decoration-line", out td))
        {
            if (td.Contains("underline")) { stil = stil with { Understrek = true }; }
            if (td.Contains("line-through")) { stil = stil with { Gjennomstrek = true }; }
        }
        if (css.TryGetValue("color", out var farge) && TilFarge(farge) is { } hex)
        {
            stil = stil with { Farge = hex };
        }

        return stil;
    }

    private static float? LesLinjeavstand(IElement el)
    {
        if (!LesCss(el).TryGetValue("line-height", out var verdi))
        {
            return null;
        }

        if (float.TryParse(verdi, NumberStyles.Float, CultureInfo.InvariantCulture, out var tall))
        {
            return Math.Clamp(tall, 0.8f, 3f);
        }
        if (verdi.EndsWith('%') && float.TryParse(verdi.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var prosent))
        {
            return Math.Clamp(prosent / 100f, 0.8f, 3f);
        }
        return null;
    }

    private static string? LesJustering(IElement el)
    {
        var css = LesCss(el);
        if (css.TryGetValue("text-align", out var verdi))
        {
            return verdi;
        }
        return el.GetAttribute("align")?.ToLowerInvariant();
    }

    private static void Juster(TextDescriptor t, string? justering)
    {
        switch (justering)
        {
            case "center": t.AlignCenter(); break;
            case "right": t.AlignRight(); break;
            case "justify": t.Justify(); break;
        }
    }

    private static Dictionary<string, string> LesCss(IElement el)
    {
        var resultat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var style = el.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(style))
        {
            return resultat;
        }

        foreach (var del in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kolon = del.IndexOf(':');
            if (kolon > 0)
            {
                resultat[del[..kolon].Trim()] = del[(kolon + 1)..].Trim().ToLowerInvariant();
            }
        }
        return resultat;
    }

    // Editoren bruker px (skjerm) - PDF-en bruker punkter (1 px = 0,75 pt).
    private static float? TilPunkter(string verdi)
    {
        var m = Regex.Match(verdi, @"^([\d.]+)\s*(px|pt|em|rem)?$");
        if (!m.Success || !float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tall))
        {
            return null;
        }

        var punkter = m.Groups[2].Value switch
        {
            "pt" => tall,
            "em" or "rem" => tall * 10,
            _ => tall * 0.75f
        };
        return Math.Clamp(punkter, 6, 48);
    }

    // Godtar #rgb, #rrggbb og rgb(r, g, b) - alt annet ignoreres.
    private static string? TilFarge(string? verdi)
    {
        if (string.IsNullOrWhiteSpace(verdi))
        {
            return null;
        }

        verdi = verdi.Trim().ToLowerInvariant();
        if (Regex.IsMatch(verdi, "^#([0-9a-f]{3}|[0-9a-f]{6})$"))
        {
            return verdi.Length == 4
                ? $"#{verdi[1]}{verdi[1]}{verdi[2]}{verdi[2]}{verdi[3]}{verdi[3]}"
                : verdi;
        }

        var rgb = Regex.Match(verdi, @"^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})");
        if (rgb.Success)
        {
            static int Del(Group g) => Math.Clamp(int.Parse(g.Value, CultureInfo.InvariantCulture), 0, 255);
            return $"#{Del(rgb.Groups[1]):x2}{Del(rgb.Groups[2]):x2}{Del(rgb.Groups[3]):x2}";
        }

        return null;
    }

    private static string NormalizeWhitespace(string text) => Regex.Replace(text, @"\s+", " ");
}
