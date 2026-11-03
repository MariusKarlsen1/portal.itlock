using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Felles mal for plukkliste, produktsammendrag og beslagsliste (liste-pdf-forslag.html):
// samme stil som tilbuds-PDF-en - tittel og prosjekt med logo til høyre, aksentlinje,
// lysegrå infostripe og tabellhode, annenhver grå/hvit rad og bunnlinje med sidetall.
public static class ListePdfStil
{
    public static readonly Color LysGra = Color.FromHex("#E9ECF0");
    public static readonly Color Stripe = Color.FromHex("#F5F6F8");
    public static readonly Color Zebra = Color.FromHex("#F7F8FA");
    public static readonly Color Linje = Color.FromHex("#E6E9EE");
    public static readonly Color Ramme = Color.FromHex("#E1E5EB");
    public static readonly Color Ink = Color.FromHex("#292927");
    public static readonly Color Mut = Color.FromHex("#6B7385");

    public static void Side(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.8f, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink));
    }

    public static void Topp(PageDescriptor page, RapportProfil org, PdfLogo logo, string tittel, string undertittel)
    {
        page.Header().Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().AlignBottom().Column(c =>
                {
                    c.Item().Text(tittel).FontSize(20).ExtraBold();
                    c.Item().PaddingTop(1).Text(undertittel).FontSize(10).FontColor(Mut);
                });
                row.ConstantItem(150).AlignBottom().AlignRight().Element(e => org.TegnLogo(e, logo, 26));
            });
            col.Item().PaddingTop(7).BorderBottom(2).BorderColor(org.Aksent);
        });
    }

    // Lysegrå stripe med nøkkelinfo (kunde, prosjekt, byggetrinn, dato, antall).
    public static void Info(ColumnDescriptor col, params (string Etikett, string Verdi)[] felter)
    {
        col.Item().PaddingBottom(12).Background(Stripe).Border(0.75f).BorderColor(Ramme).CornerRadius(4)
            .PaddingVertical(7).PaddingHorizontal(9).Row(row =>
            {
                // Første (kunde) og siste (antall) felt får litt mer plass.
                for (var i = 0; i < felter.Length; i++)
                {
                    var (etikett, verdi) = felter[i];
                    var bredde = i == 0 ? 1.8f : i == felter.Length - 1 ? 1.5f : 1f;
                    row.RelativeItem(bredde).PaddingRight(6).Column(c =>
                    {
                        c.Item().Text(etikett.ToUpperInvariant()).FontSize(6.5f).Bold().FontColor(Mut).LetterSpacing(0.06f);
                        c.Item().PaddingTop(1).Text(verdi).FontSize(9).Bold();
                    });
                }
            });
    }

    public static IContainer Hode(IContainer cell) => cell.Background(LysGra).PaddingVertical(5).PaddingHorizontal(6);

    public static void HodeTekst(IContainer cell, string tekst, bool hoyre = false, bool midt = false)
    {
        var c = Hode(cell);
        c = hoyre ? c.AlignRight() : midt ? c.AlignCenter() : c;
        c.Text(tekst.ToUpperInvariant()).FontSize(7.2f).Bold().LetterSpacing(0.03f);
    }

    public static IContainer Rad(IContainer cell, bool zebra) =>
        cell.Background(zebra ? Zebra : Colors.White).BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(6);

    // Varenavn med varenr (og evt. beslagstype) i grått under.
    public static void Vare(IContainer cell, string navn, params string?[] under)
    {
        cell.Column(c =>
        {
            c.Item().Text(navn).FontSize(8.8f).SemiBold();
            var tekst = string.Join(" · ", under.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (tekst.Length > 0)
            {
                c.Item().Text(tekst).FontSize(7.2f).FontColor(Mut);
            }
        });
    }

    public static IContainer SumRad(IContainer cell) => cell.Background(LysGra).PaddingVertical(5).PaddingHorizontal(6);

    public static string Levering(LevertAv levertAv) => levertAv switch
    {
        LevertAv.Ld => "Dørprodusent",
        LevertAv.Le => "Elektro",
        _ => "Fra oss"
    };

    public static void Bunn(PageDescriptor page, RapportProfil org, string tittel)
    {
        page.Footer().PaddingTop(8).BorderTop(0.6f).BorderColor(Ramme).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text(org.Bunntekst).FontSize(7).FontColor(Mut);
            row.AutoItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(Mut));
                t.Span($"{tittel} · side ");
                t.CurrentPageNumber();
                t.Span(" av ");
                t.TotalPages();
            });
        });
    }
}
