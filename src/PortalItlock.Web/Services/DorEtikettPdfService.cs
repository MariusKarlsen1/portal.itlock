using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// QR-etikett for én dør (QR-kode-fanen i dørkortet): 70 × 40 mm med firmanavn og
// prosjekt øverst, QR-koden, dørnummer, rom og etasje. Passer på labelskriver eller
// kan klippes ut av et vanlig ark. QR-koden peker på /dor/{id}, som sender
// vaktmestere med driftslisens videre til driftsportalen.
public class DorEtikettPdfService(ApplicationDbContext db, ITenantContext tenantContext)
{
    private const float BreddeMm = 70f;
    private const float HoydeMm = 40f;

    public async Task<byte[]?> GenerateAsync(int dorId, string baseUrl)
    {
        var dor = await db.Dorer.AsNoTracking()
            .Include(d => d.Prosjekt)
            .FirstOrDefaultAsync(d => d.Id == dorId);
        if (dor is null)
        {
            return null;
        }

        var org = RapportProfil.Fra(tenantContext.Current);
        var qr = QrCodeService.GeneratePng($"{baseUrl}/dor/{dor.Id}");
        var rom = string.Join(" · ", new[] { dor.Romnr, dor.DorTil }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var etasje = string.Join(" · ", new[]
        {
            string.IsNullOrWhiteSpace(dor.Etasje) ? null : $"{dor.Etasje}. etg",
            string.IsNullOrWhiteSpace(dor.Sone) ? null : $"sone {dor.Sone}"
        }.Where(s => s is not null));

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(BreddeMm, HoydeMm, Unit.Millimetre);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontSize(7).FontColor("#14213D"));

                page.Content().Column(col =>
                {
                    col.Item().Background("#0F1B33").PaddingVertical(1.6f, Unit.Millimetre).PaddingHorizontal(2.5f, Unit.Millimetre).Row(rad =>
                    {
                        rad.RelativeItem().Text(org.Navn).FontColor(Colors.White).Bold().FontSize(7).ClampLines(1);
                        rad.AutoItem().AlignRight().Text(dor.Prosjekt?.Navn ?? "").FontColor("#C3CCDD").FontSize(6.5f).ClampLines(1);
                    });

                    col.Item().PaddingHorizontal(2.5f, Unit.Millimetre).PaddingTop(1.8f, Unit.Millimetre).Row(rad =>
                    {
                        rad.ConstantItem(22, Unit.Millimetre).Height(22, Unit.Millimetre).Image(qr).FitArea();
                        rad.ConstantItem(3, Unit.Millimetre);
                        rad.RelativeItem().AlignMiddle().Column(tekst =>
                        {
                            tekst.Item().Text(dor.Dornummer).Bold().FontSize(18).ClampLines(1);
                            if (rom.Length > 0)
                            {
                                tekst.Item().Text(rom).FontSize(7).FontColor("#545C65").ClampLines(2);
                            }
                            if (etasje.Length > 0)
                            {
                                tekst.Item().Text(etasje).FontSize(7).FontColor("#545C65").ClampLines(1);
                            }
                        });
                    });

                    col.Item().ExtendVertical().AlignBottom().BorderTop(0.4f).BorderColor("#C9CFD9")
                        .PaddingVertical(1.2f, Unit.Millimetre).AlignCenter()
                        .Text("Skann for service eller for å melde feil").FontSize(6).FontColor("#545C65");
                });
            });
        });

        return document.GeneratePdf();
    }
}
