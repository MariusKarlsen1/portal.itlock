using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class PlukklistePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var behov = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0
                && (byggetrinn == null || (k.Dor!.Plantegning != null && k.Dor!.Plantegning!.Byggetrinn == byggetrinn)))
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
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => pdfLogo.Render(e, 15));
                        row.RelativeItem().AlignRight().Text($"Plukkliste – {prosjekt.Navn}{(byggetrinn is not null ? $" ({byggetrinn})" : "")}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Text("Plukkliste").FontSize(16).SemiBold();

                    col.Item().PaddingTop(6).Table(table =>
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
                            IContainer Hode() => header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).PaddingRight(4);

                            Hode().Text("#").Bold();
                            Hode().Text("Beslagstype").Bold();
                            Hode().Text("Varenr").Bold();
                            Hode().Text("Varenavn").Bold();
                            Hode().Text("Overflate").Bold();
                            Hode().Text("Enhet").Bold();
                            Hode().Text("Antall").Bold();
                            Hode().Text("Plukket").Bold();
                            Hode().Text("Bestilt").Bold();
                            Hode().Text("Må bestilles").Bold();
                        });

                        var i = 0;
                        foreach (var g in gruppert)
                        {
                            i++;
                            var linje = linjer.FirstOrDefault(l => l.ComponentId == g.ComponentId);
                            var plukket = linje?.AntallPlukket ?? 0;
                            var bestilt = linje?.VarerBestilt ?? 0;
                            var maBestilles = Math.Max(g.Antall - bestilt, 0);

                            IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(4);

                            Rad().Text(i.ToString());
                            Rad().Text(g.Component.Type?.Navn ?? "");
                            Rad().Text(g.Component.Produktkode ?? "");
                            Rad().Text(g.Component.Navn);
                            Rad().Text(g.Component.Overflate ?? "");
                            Rad().Text(g.Component.Enhet ?? "Stk");
                            Rad().Text(g.Antall.ToString());
                            Rad().Text(plukket.ToString());
                            Rad().Text(bestilt.ToString());
                            var maBestillesCelle = Rad().Text(maBestilles.ToString());
                            if (maBestilles > 0)
                            {
                                maBestillesCelle.Bold();
                            }
                        }
                    });
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
