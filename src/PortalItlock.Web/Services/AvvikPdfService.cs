using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class AvvikPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

    public async Task<byte[]?> GenerateAsync(int avvikId)
    {
        var avvik = await db.Avvik
            .Include(a => a.Dor).ThenInclude(d => d!.Prosjekt)
            .FirstOrDefaultAsync(a => a.Id == avvikId);

        if (avvik is null)
        {
            return null;
        }

        var dor = avvik.Dor;
        var prosjektNavn = dor?.Prosjekt?.Navn ?? "";

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Avviksmelding").FontSize(20).SemiBold();
                        row.RelativeItem().AlignRight().Element(e => pdfLogo.Render(e, 16));
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Text(t =>
                    {
                        t.Span("Prosjekt: ").Bold();
                        t.Span(prosjektNavn);
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("Dør: ").Bold();
                        t.Span(dor?.Dornummer ?? "");
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("Dato: ").Bold();
                        t.Span(avvik.OpprettetDato.ToString("dd.MM.yyyy"));
                    });

                    col.Item().PaddingTop(10).Text("Beskrivelse av avvik").Bold();
                    col.Item().Text(avvik.Beskrivelse);

                    if (!string.IsNullOrWhiteSpace(avvik.UtbedringBeskrivelse))
                    {
                        col.Item().PaddingTop(10).Text("Foreslått utbedring").Bold();
                        col.Item().Text(avvik.UtbedringBeskrivelse);
                    }

                    if (avvik.Pris.HasValue)
                    {
                        col.Item().PaddingTop(10).Text(t =>
                        {
                            t.Span("Pris for utbedring: ").Bold();
                            t.Span($"{avvik.Pris.Value.ToString("N2", Kultur)} kr eks. mva");
                        });
                    }

                    col.Item().PaddingTop(30).Text("Godkjenning").Bold();
                    if (avvik.Signatur is not null)
                    {
                        col.Item().PaddingTop(6).Width(200).Image(avvik.Signatur);
                        col.Item().Text($"{avvik.SignertAvNavn} - signert {avvik.SignertDato?.ToString("dd.MM.yyyy HH:mm")}");
                    }
                    else
                    {
                        col.Item().PaddingTop(30).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().Text("Signatur / navn / dato");
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Column(c =>
                {
                    c.Item().AlignCenter().Text($"{FirmaNavn} - {FirmaAdresse} - Tlf {FirmaTelefon} - {FirmaEpost}").FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }
}
