using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Felles mal D1/U1 (plantegning-pdf-forslag.html) for dørplantegningen og
// utstyr og kabeltrekk: logo og tittel i topp med strek i organisasjonens farge,
// prosjekt/kunde/tegning/dato på én rad, tegningen så stor som mulig og
// tegnforklaring + bunntekst fra organisasjonen nederst.
public static class PlantegningPdfMal
{
    public static readonly Color Dempet = Color.FromHex("#6B6863");
    public static readonly Color Svak = Color.FromHex("#8A857C");
    public static readonly Color Ramme = Color.FromHex("#E3DCCF");

    public static void Side(PageDescriptor page)
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(1.2f, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(PdfStil.Ink));
    }

    public static void Topp(IContainer container, RapportProfil org, PdfLogo pdfLogo, string tittel, Plantegning plantegning)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().AlignBottom().Element(e => org.TegnLogo(e, pdfLogo, 22));
                row.RelativeItem().AlignBottom().AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(tittel).FontSize(13).Bold();
                    c.Item().AlignRight().Text($"{plantegning.Prosjekt?.Navn} · {plantegning.Navn}").FontSize(8).FontColor(Dempet);
                });
            });
            col.Item().PaddingTop(5).LineHorizontal(1.5f).LineColor(org.Aksent);
            col.Item().PaddingTop(6).Row(row =>
            {
                void Felt(string etikett, string? verdi) => row.AutoItem().PaddingRight(22).Column(c =>
                {
                    c.Item().Text(etikett.ToUpperInvariant()).FontSize(5.5f).Bold().FontColor(org.Aksent).LetterSpacing(0.08f);
                    c.Item().Text(string.IsNullOrWhiteSpace(verdi) ? "–" : verdi).FontSize(8).SemiBold();
                });
                Felt("Prosjekt", plantegning.Prosjekt?.Navn);
                Felt("Kunde", plantegning.Prosjekt?.Kunde?.Navn);
                Felt("Tegning", string.IsNullOrWhiteSpace(plantegning.Byggetrinn) ? plantegning.Navn : $"{plantegning.Navn} · {plantegning.Byggetrinn}");
                Felt("Dato", DateTime.Now.ToString("dd.MM.yyyy"));
            });
        });
    }

    // Tegningen fyller resten av siden (siste element i innholdet).
    public static void Tegning(IContainer container, string svg) =>
        container.PaddingTop(8).Border(0.6f).BorderColor(Ramme).Padding(4).AlignCenter().AlignMiddle().Svg(svg).FitArea();

    public static void Bunn(IContainer container, RapportProfil org, string tittel, Plantegning plantegning, Action<ColumnDescriptor> forklaring)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(6).Column(forklaring);
            col.Item().PaddingTop(6).BorderTop(0.5f).BorderColor(Ramme).PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(org.Bunntekst).FontSize(7).FontColor(Svak);
                row.AutoItem().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontSize(7).FontColor(Svak));
                    x.Span($"{tittel} · {plantegning.Navn} · side ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });
    }

    // En rad med forklaringer som brytes over flere linjer ved behov.
    public static void ForklaringRad(ColumnDescriptor col, IEnumerable<Action<IContainer>> elementer) =>
        col.Item().Inlined(inl =>
        {
            inl.HorizontalSpacing(14);
            inl.VerticalSpacing(3);
            foreach (var e in elementer)
            {
                inl.Item().Element(e);
            }
        });
}
