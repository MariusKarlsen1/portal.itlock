using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class DorBeslagslistePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var dorer = await db.Dorer
            .Where(d => d.ProsjektId == prosjektId
                && (valgteByggetrinn == null || (d.Plantegning != null && valgteByggetrinn.Contains(d.Plantegning.Byggetrinn))))
            .Include(d => d.Komponenter).ThenInclude(k => k.Component).ThenInclude(c => c!.Type)
            .Include(d => d.Funksjoner)
            .OrderBy(d => d.Dornummer)
            .ToListAsync();

        dorer = dorer.Where(d => d.Komponenter.Any(k => k.Component is not null)).ToList();

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
                        row.RelativeItem().Element(e => pdfLogo.Render(e, 15));
                        row.RelativeItem().AlignRight().Text($"Beslagsliste – {prosjekt.Navn}{(valgteByggetrinn is not null ? $" ({string.Join(", ", valgteByggetrinn)})" : "")}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    if (dorer.Count == 0)
                    {
                        col.Item().Text("Ingen dører med registrert beslag i dette prosjektet.").FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    for (var i = 0; i < dorer.Count; i++)
                    {
                        DorPdfSeksjoner.RenderDorSide(col, dorer[i], visPris: false, hentUtpris: _ => 0m);
                        if (i < dorer.Count - 1)
                        {
                            col.Item().PageBreak();
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
