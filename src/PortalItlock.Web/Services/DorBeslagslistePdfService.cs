using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class DorBeslagslistePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
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

        // Mal L14: avsenderen (logo, farge, bunntekst) er organisasjonen som er logget inn.
        var org = RapportProfil.Fra(tenantContext.Current);

        // B1 (liste-pdf-forslag.html): dørkortene som i tilbuds-PDF-en (lysegrå topp, uten
        // status). Er det plass til flere dører på en side står de etter hverandre; et dørkort
        // som ikke får plass flyttes til neste side i stedet for å deles.
        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                ListePdfStil.Side(page);
                var undertittel = string.Join(" · ", new[] { prosjekt.Navn, valgteByggetrinn is not null ? $"Byggetrinn {string.Join(", ", valgteByggetrinn)}" : null }.Where(s => !string.IsNullOrWhiteSpace(s)));
                ListePdfStil.Topp(page, org, pdfLogo, "Beslagsliste", undertittel);

                page.Content().PaddingTop(12).Column(col =>
                {
                    if (dorer.Count == 0)
                    {
                        col.Item().Text("Ingen dører med registrert beslag i dette prosjektet.").FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    for (var i = 0; i < dorer.Count; i++)
                    {
                        var dor = dorer[i];
                        col.Item().PaddingTop(i == 0 ? 0 : 16).PreventPageBreak().Column(inner =>
                            DorPdfSeksjoner.RenderDorSide(inner, dor, visPris: false, hentUtpris: _ => 0m,
                                hodeBak: ListePdfStil.LysGra, hodeTekst: ListePdfStil.Ink, visStatus: false, fullLevering: true));
                    }
                });

                ListePdfStil.Bunn(page, org, "Beslagsliste");
            });
        });

        return document.GeneratePdf();
    }
}
