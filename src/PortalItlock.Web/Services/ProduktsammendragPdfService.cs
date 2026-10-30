using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class ProduktsammendragPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);
        var komponenter = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component).ThenInclude(c => c!.Type)
            .ToListAsync();

        var rader = komponenter
            .Where(k => k.Component is not null)
            .GroupBy(k => new { k.ComponentId, k.LevertAv })
            .Select(g => new
            {
                Component = g.First().Component!,
                g.Key.LevertAv,
                Antall = g.Sum(x => x.Antall)
            })
            .OrderBy(r => r.Component.Type?.Navn)
            .ThenBy(r => r.Component.Navn)
            .ToList();

        if (rader.Count == 0)
        {
            return null;
        }

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, RapportProfil.Fra(tenantContext.Current), pdfLogo, 1.8f, "Produktsammendrag", prosjekt.Navn));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(2.8f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        });

                        table.Header(header =>
                        {
                            IContainer Hode() => PdfStil.TabellHode(header.Cell());

                            Hode().Text("Beslagstype").FontSize(8).Bold().FontColor(RapportMal.Svak);
                            Hode().Text("Varenr").FontSize(8).Bold().FontColor(RapportMal.Svak);
                            Hode().Text("Varenavn").FontSize(8).Bold().FontColor(RapportMal.Svak);
                            Hode().Text("Overflate").FontSize(8).Bold().FontColor(RapportMal.Svak);
                            Hode().Text("Enhet").FontSize(8).Bold().FontColor(RapportMal.Svak);
                            Hode().Text("Levering").FontSize(8).Bold().FontColor(RapportMal.Svak);
                            Hode().Text("Antall").FontSize(8).Bold().FontColor(RapportMal.Svak);
                        });

                        var i = 0;
                        foreach (var r in rader)
                        {
                            var alt = i++ % 2 == 1;
                            IContainer Rad() => PdfStil.TabellRad(table.Cell(), alt);

                            Rad().Text(r.Component.Type?.Navn ?? "").FontSize(8.5f);
                            Rad().Text(r.Component.Produktkode ?? "").FontSize(8.5f);
                            Rad().Text(r.Component.Navn).FontSize(8.5f);
                            Rad().Text(r.Component.Overflate ?? "").FontSize(8.5f);
                            Rad().Text(r.Component.Enhet ?? "Stk").FontSize(8.5f);
                            Rad().Text(r.LevertAv.Visningsnavn()).FontSize(8.5f);
                            Rad().Text(r.Antall.ToString()).FontSize(8.5f);
                        }
                    });
                });

                RapportMal.Bunn(page.Footer(), RapportProfil.Fra(tenantContext.Current));
            });
        });

        return document.GeneratePdf();
    }
}
