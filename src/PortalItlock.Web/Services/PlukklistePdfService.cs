using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class PlukklistePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<List<LagretPdfLinje>> GetSnapshotLinjerAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var behov = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component)
            .ToListAsync();

        return behov
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

        var behov = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component).ThenInclude(c => c!.Type)
            .ToListAsync();

        var gruppert = behov
            .Where(k => k.Component is not null)
            .GroupBy(k => k.ComponentId)
            .Select(g => new { ComponentId = g.Key, Component = g.First().Component!, Antall = g.Sum(x => x.Antall) })
            .OrderBy(x => x.Component.Type?.Navn)
            .ThenBy(x => x.Component.Navn)
            .ToList();

        if (gruppert.Count == 0)
        {
            return null;
        }

        var linjer = await db.PlukklisteLinjer
            .Where(p => p.ProsjektId == prosjektId)
            .ToListAsync();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                var byggetrinnTekst = valgteByggetrinn is not null ? $"Byggetrinn {string.Join(", ", valgteByggetrinn)}" : null;
                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Plukkliste", string.Join(" · ", new[] { prosjekt.Navn, byggetrinnTekst }.Where(s => !string.IsNullOrWhiteSpace(s)))));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(20);
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            IContainer Hode() => PdfStil.TabellHode(header.Cell());

                            Hode().Text("#").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Beslagstype").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Varenr").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Varenavn").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Overflate").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Enhet").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Antall").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Plukket").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Bestilt").FontSize(8).Bold().FontColor(Colors.White);
                            Hode().Text("Må bestilles").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        var i = 0;
                        foreach (var g in gruppert)
                        {
                            i++;
                            var linje = linjer.FirstOrDefault(l => l.ComponentId == g.ComponentId);
                            var plukket = linje?.AntallPlukket ?? 0;
                            var bestilt = linje?.VarerBestilt ?? 0;
                            var maBestilles = Math.Max(g.Antall - bestilt, 0);
                            var alt = i % 2 == 0;

                            IContainer Rad() => PdfStil.TabellRad(table.Cell(), alt);

                            Rad().Text(i.ToString()).FontSize(8.5f);
                            Rad().Text(g.Component.Type?.Navn ?? "").FontSize(8.5f);
                            Rad().Text(g.Component.Produktkode ?? "").FontSize(8.5f);
                            Rad().Text(g.Component.Navn).FontSize(8.5f);
                            Rad().Text(g.Component.Overflate ?? "").FontSize(8.5f);
                            Rad().Text(g.Component.Enhet ?? "Stk").FontSize(8.5f);
                            Rad().Text(g.Antall.ToString()).FontSize(8.5f);
                            Rad().Text(plukket.ToString()).FontSize(8.5f);
                            Rad().Text(bestilt.ToString()).FontSize(8.5f);
                            var maBestillesCelle = Rad().Text(maBestilles.ToString()).FontSize(8.5f);
                            if (maBestilles > 0)
                            {
                                maBestillesCelle.Bold().FontColor(Color.FromHex("#B5502D"));
                            }
                        }
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
