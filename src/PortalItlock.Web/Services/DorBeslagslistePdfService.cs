using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class DorBeslagslistePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<List<LagretPdfLinje>> GetSnapshotLinjerAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var dorer = await db.Dorer
            .Where(d => d.ProsjektId == prosjektId
                && (valgteByggetrinn == null || (d.Plantegning != null && valgteByggetrinn.Contains(d.Plantegning.Byggetrinn))))
            .Include(d => d.Komponenter).ThenInclude(k => k.Component)
            .ToListAsync();

        return dorer
            .SelectMany(d => d.Komponenter)
            .Where(k => k.Component is not null)
            .GroupBy(k => k.Component!.Navn)
            .Select(g => new LagretPdfLinje(g.Key, g.Sum(x => x.Antall), null))
            .OrderBy(l => l.Navn)
            .ToList();
    }

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
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                var undertittel = string.Join(" · ", new[] { prosjekt.Navn, valgteByggetrinn is not null ? $"Byggetrinn {string.Join(", ", valgteByggetrinn)}" : null }.Where(s => !string.IsNullOrWhiteSpace(s)));
                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Beslagsliste", undertittel));

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

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
