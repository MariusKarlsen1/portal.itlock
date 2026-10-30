using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Delt visuelt språk for genererte PDF-er: rolig, lys palett (lys sand,
// tynne varme linjer og en dempet mørk aksent, malen «Sandbånd») som alle
// rapporter og dokumenter i portalen bruker, i stedet for at hver enkelt
// *PdfService finner opp sin egen stil.
public static class PdfStil
{
    public static readonly Color Sand = Color.FromHex("#F7F4EF");
    public static readonly Color SandBorder = Color.FromHex("#E7E1D7");
    public static readonly Color Accent = Color.FromHex("#4A4540");
    public static readonly Color Ink = Color.FromHex("#292927");

    // Toppen i malen «Sandbånd»: lyst bånd i organisasjonens farge på første
    // side, diskret løpetopp på resten. marg er sidens marg i cm.
    public static void Header(ColumnDescriptor col, RapportProfil org, PdfLogo logo, float marg, string tittel, string? undertittel = null)
    {
        var pt = marg * 28.3465f;
        col.Item().ShowOnce().PaddingBottom(4).Element(e => RapportMal.Band(e, org, logo, tittel, undertittel ?? tittel, null,
            hoyre: NorskTid.Idag.ToString("dd.MM.yyyy"), margH: pt, margTopp: pt));
        col.Item().SkipOnce().Element(e => RapportMal.Lopetopp(e, org, tittel, undertittel));
    }

    public static void InfoBoks(ColumnDescriptor col, int kolonner, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().BorderBottom(0.75f).BorderColor(SandBorder).PaddingBottom(4).Table(table =>
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
                    c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).Bold().FontColor(RapportMal.Svak).LetterSpacing(0.07f);
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
        cell.BorderBottom(1.2f).BorderColor(SandBorder).PaddingVertical(5).PaddingHorizontal(6);

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
