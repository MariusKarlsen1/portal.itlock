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
/// Delt mellom tilbuds-PDFen og FDV-PDFen slik at begge kan bruke samme forside-maler.
/// </summary>
public static class ForsideRenderer
{
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
                        t.DefaultTextStyle(x => x.LineHeight(1.35f));
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
                    col.Item().PaddingTop(8).PaddingBottom(2).Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(19).SemiBold());
                        RenderInlineChildren(t, el);
                    });
                    break;

                case "h2":
                    col.Item().PaddingTop(6).PaddingBottom(2).Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(14).Bold());
                        RenderInlineChildren(t, el);
                    });
                    break;

                case "h3":
                    col.Item().PaddingTop(4).PaddingBottom(2).Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(11).Bold());
                        RenderInlineChildren(t, el);
                    });
                    break;

                case "ul":
                    RenderList(col, el, ordered: false);
                    break;

                case "ol":
                    RenderList(col, el, ordered: true);
                    break;

                case "br":
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(el.TextContent))
                    {
                        col.Item().Height(6);
                        break;
                    }

                    col.Item().PaddingBottom(4).Text(t =>
                    {
                        t.DefaultTextStyle(x => x.LineHeight(1.35f));
                        RenderInlineChildren(t, el);
                    });
                    break;
            }
        }
    }

    private static void RenderList(ColumnDescriptor col, IElement listEl, bool ordered)
    {
        col.Item().PaddingBottom(4).Column(listCol =>
        {
            var index = 1;
            foreach (var child in listEl.Children)
            {
                if (!string.Equals(child.TagName, "li", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var marker = ordered ? $"{index}." : "•";
                index++;

                listCol.Item().PaddingBottom(2).Row(row =>
                {
                    row.ConstantItem(16).Text(marker);
                    row.RelativeItem().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.LineHeight(1.3f));
                        RenderInlineChildren(t, child);
                    });
                });
            }
        });
    }

    private static void RenderInlineChildren(TextDescriptor t, IElement el)
    {
        foreach (var child in el.ChildNodes)
        {
            RenderInlineNode(t, child, bold: false, italic: false, underline: false);
        }
    }

    private static void RenderInlineNode(TextDescriptor t, INode node, bool bold, bool italic, bool underline)
    {
        if (node is IText text)
        {
            var value = NormalizeWhitespace(text.TextContent);
            if (value.Length == 0)
            {
                return;
            }

            var span = t.Span(value);
            if (bold)
            {
                span.Bold();
            }

            if (italic)
            {
                span.Italic();
            }

            if (underline)
            {
                span.Underline();
            }

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
                if (!string.IsNullOrWhiteSpace(href))
                {
                    t.Hyperlink(el.TextContent, href);
                    return;
                }

                break;

            case "strong":
            case "b":
                bold = true;
                break;

            case "em":
            case "i":
                italic = true;
                break;

            case "u":
                underline = true;
                break;
        }

        foreach (var child in el.ChildNodes)
        {
            RenderInlineNode(t, child, bold, italic, underline);
        }
    }

    private static string NormalizeWhitespace(string text) => Regex.Replace(text, @"\s+", " ");
}
