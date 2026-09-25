using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Delt visuelt sprak for genererte PDF-er - samme palett som allerede ble
// etablert i CeGodkjenningPdfService (sand-bakgrunn, aksentkant, hvit-pa-
// aksent for det viktigste), gjenbrukt her sa alle rapporter/dokumenter i
// portalen far konsekvent merkevarebygging i stedet for at hver enkelt
// *PdfService finner opp sin egen gra standard-stil.
public static class PdfStil
{
    public static readonly Color Sand = Color.FromHex("#F2EBE1");
    public static readonly Color SandBorder = Color.FromHex("#E4D9C8");
    public static readonly Color Accent = Color.FromHex("#835E41");
    public static readonly Color Ink = Color.FromHex("#292927");

    public static void Header(ColumnDescriptor col, PdfLogo logo, string tittel, string? undertittel = null)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(tittel).FontSize(18).Bold().FontColor(Ink);
                if (!string.IsNullOrWhiteSpace(undertittel))
                {
                    c.Item().PaddingTop(2).Text(undertittel).FontSize(9).FontColor(Colors.Grey.Darken1);
                }
            });
            row.ConstantItem(120).AlignRight().Element(e => logo.Render(e, 22));
        });
        col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Accent);
    }

    public static void FooterRad(RowDescriptor row)
    {
        row.RelativeItem().Text($"{FirmaInfo.Navn} - {FirmaInfo.AdresseFull} - Tlf {FirmaInfo.Telefon}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
        row.RelativeItem().AlignRight().Text(t =>
        {
            t.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            t.Span(" / ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            t.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
        });
    }

    public static void InfoBoks(ColumnDescriptor col, int kolonner, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().Background(Sand).Border(1).BorderColor(SandBorder).Padding(10).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                for (var i = 0; i < kolonner; i++)
                {
                    c.RelativeColumn();
                }
            });

            foreach (var (label, verdi) in utfylte)
            {
                table.Cell().Padding(3).Column(c =>
                {
                    c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).Bold().FontColor(Accent).LetterSpacing(0.03f);
                    c.Item().PaddingTop(1).Text(verdi).FontSize(9.3f).SemiBold();
                });
            }
        });
    }

    public static void SeksjonTittel(ColumnDescriptor col, string tittel)
    {
        col.Item().Text(tittel.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(Accent).LetterSpacing(0.03f);
        col.Item().PaddingBottom(2).BorderBottom(0.75f).BorderColor(SandBorder);
    }

    public static void FriSeksjon(ColumnDescriptor col, string tittel, string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return;
        }

        SeksjonTittel(col, tittel);
        col.Item().PaddingTop(3).Text(tekst).FontSize(9);
    }

    public static void NotatBoks(ColumnDescriptor col, string label, string tekst)
    {
        col.Item().Background(Sand).BorderLeft(2.5f).BorderColor(Accent).Padding(7).Text(t =>
        {
            t.Span($"{label}: ").SemiBold();
            t.Span(tekst);
        });
    }

    public static IContainer TabellHode(IContainer cell) =>
        cell.Background(Accent).PaddingVertical(5).PaddingHorizontal(6);

    public static IContainer TabellRad(IContainer cell, bool alternerende) =>
        cell.Background(alternerende ? Sand : Colors.White).BorderBottom(0.5f).BorderColor(SandBorder).PaddingVertical(4).PaddingHorizontal(6);

    public static void SjekkRad(ColumnDescriptor col, bool fullfort, string tekst, string? underTekst = null)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(16).Element(e =>
            {
                if (fullfort)
                {
                    e.Width(11).Height(11).Background(Accent).AlignMiddle().AlignCenter().Text("X").FontSize(7).Bold().FontColor(Colors.White);
                }
                else
                {
                    e.Width(11).Height(11).Border(1.2f).BorderColor(Accent);
                }
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(tekst).FontSize(9).FontColor(fullfort ? Ink : Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(underTekst))
                {
                    c.Item().Text(underTekst).FontSize(7.5f).FontColor(Colors.Grey.Medium);
                }
            });
        });
    }
}
