using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace PortalItlock.Web.Services;

public class FdvPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<List<Component>> HentKomponenterMedFdvAsync(int prosjektId)
    {
        return await db.DorKomponenter
            .Where(dk => dk.Dor!.ProsjektId == prosjektId && dk.Component!.FdvData != null)
            .Select(dk => dk.Component!)
            .Distinct()
            .OrderBy(c => c.Navn)
            .ToListAsync();
    }

    public async Task<byte[]?> GenerateAsync(int prosjektId)
    {
        var komponenter = await HentKomponenterMedFdvAsync(prosjektId);
        if (komponenter.Count == 0)
        {
            return null;
        }

        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);

        var document = Document.Create(doc =>
        {
            if (!string.IsNullOrWhiteSpace(prosjekt?.FdvForside))
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
                            row.RelativeItem().AlignRight().Text($"FDV – {prosjekt.Navn}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(14).Column(col => ForsideRenderer.Render(col, prosjekt.FdvForside));
                });
            }

            foreach (var komponent in komponenter)
            {
                List<SKBitmap> sider;
                try
                {
                    sider = PDFtoImage.Conversion.ToImages(komponent.FdvData!, options: new PDFtoImage.RenderOptions(Dpi: 150)).ToList();
                }
                catch (Exception)
                {
                    continue;
                }

                for (var i = 0; i < sider.Count; i++)
                {
                    using var bitmap = sider[i];
                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 85);
                    var bildeData = encoded.ToArray();

                    doc.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);

                        if (i == 0)
                        {
                            page.Header().PaddingBottom(6).Text(t =>
                            {
                                t.Span("FDV – ").SemiBold();
                                t.Span(komponent.Navn);
                                if (!string.IsNullOrWhiteSpace(komponent.Produktkode))
                                {
                                    t.Span($" ({komponent.Produktkode})").FontColor(Colors.Grey.Darken1);
                                }
                            });
                        }

                        page.Content().AlignCenter().AlignMiddle().Image(bildeData).FitArea();
                    });
                }
            }
        });

        return document.GeneratePdf();
    }
}
