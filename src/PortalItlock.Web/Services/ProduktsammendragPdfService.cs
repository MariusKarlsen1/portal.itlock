using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class ProduktsammendragPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<byte[]?> GenerateAsync(int prosjektId)
    {
        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var komponenter = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.ComponentId != 0)
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
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => pdfLogo.Render(e, 15));
                        row.RelativeItem().AlignRight().Text($"Produktsammendrag – {prosjekt.Navn}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Text("Produktsammendrag").FontSize(16).SemiBold();

                    col.Item().PaddingTop(6).Table(table =>
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
                            IContainer Hode() => header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).PaddingRight(4);

                            Hode().Text("Beslagstype").Bold();
                            Hode().Text("Varenr").Bold();
                            Hode().Text("Varenavn").Bold();
                            Hode().Text("Overflate").Bold();
                            Hode().Text("Enhet").Bold();
                            Hode().Text("Levering").Bold();
                            Hode().Text("Antall").Bold();
                        });

                        foreach (var r in rader)
                        {
                            IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(4);

                            Rad().Text(r.Component.Type?.Navn ?? "");
                            Rad().Text(r.Component.Produktkode ?? "");
                            Rad().Text(r.Component.Navn);
                            Rad().Text(r.Component.Overflate ?? "");
                            Rad().Text(r.Component.Enhet ?? "Stk");
                            Rad().Text(r.LevertAv.Visningsnavn());
                            Rad().Text(r.Antall.ToString());
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
