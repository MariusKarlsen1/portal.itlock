using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace PortalItlock.Web.Services;

public class FdvPdfService(ApplicationDbContext db)
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

        var document = Document.Create(doc =>
        {
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
