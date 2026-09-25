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
    public async Task<List<Component>> HentKomponenterMedFdvAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var komponentIder = await db.DorKomponenter
            .Where(dk => dk.Dor!.ProsjektId == prosjektId && dk.Component!.FdvData != null
                && (valgteByggetrinn == null || (dk.Dor!.Plantegning != null && valgteByggetrinn.Contains(dk.Dor!.Plantegning!.Byggetrinn))))
            .Select(dk => dk.ComponentId)
            .Distinct()
            .ToListAsync();

        return await db.Components
            .Where(c => komponentIder.Contains(c.Id))
            .Include(c => c.FdvDokumenter)
            .OrderBy(c => c.Navn)
            .ToListAsync();
    }

    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);
        var komponenter = await HentKomponenterMedFdvAsync(prosjektId, byggetrinn);
        var ekstraVedlegg = await db.FdvVedlegg
            .Where(v => v.ProsjektId == prosjektId)
            .OrderBy(v => v.Navn)
            .ToListAsync();

        if (komponenter.Count == 0 && ekstraVedlegg.Count == 0)
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
                    page.Margin(1.8f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                    var undertittel = string.Join(" · ", new[] { prosjekt.Navn, valgteByggetrinn is not null ? $"Byggetrinn {string.Join(", ", valgteByggetrinn)}" : null }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    page.Header().Column(col => PdfStil.Header(col, pdfLogo, "FDV-dokumentasjon", undertittel));

                    page.Content().PaddingTop(14).Column(col => ForsideRenderer.Render(col, prosjekt.FdvForside));
                });
            }

            void LeggTilPdfSider(byte[] pdfData, string tittel)
            {
                List<SKBitmap> sider;
                try
                {
                    sider = PDFtoImage.Conversion.ToImages(pdfData, options: new PDFtoImage.RenderOptions(Dpi: 150)).ToList();
                }
                catch (Exception)
                {
                    return;
                }

                for (var i = 0; i < sider.Count; i++)
                {
                    using var bitmap = sider[i];
                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 85);
                    var bildeData = encoded.ToArray();
                    var erForsteSide = i == 0;

                    doc.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);

                        if (erForsteSide)
                        {
                            page.Header().PaddingBottom(6).Background(PdfStil.Sand).Padding(6).Text(tittel).FontSize(9).Bold().FontColor(PdfStil.Accent);
                        }

                        page.Content().AlignCenter().AlignMiddle().Image(bildeData).FitArea();
                    });
                }
            }

            foreach (var komponent in komponenter)
            {
                var tittel = $"FDV – {komponent.Navn}" + (!string.IsNullOrWhiteSpace(komponent.Produktkode) ? $" ({komponent.Produktkode})" : "");
                LeggTilPdfSider(komponent.FdvData!, tittel);

                foreach (var ekstraDokument in komponent.FdvDokumenter)
                {
                    LeggTilPdfSider(ekstraDokument.Data, $"FDV – {komponent.Navn} - {ekstraDokument.Filnavn}");
                }
            }

            foreach (var vedlegg in ekstraVedlegg)
            {
                LeggTilPdfSider(vedlegg.Data, $"FDV – {vedlegg.Navn}");
            }
        });

        return document.GeneratePdf();
    }
}
