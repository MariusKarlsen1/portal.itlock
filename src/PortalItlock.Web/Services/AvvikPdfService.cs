using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class AvvikPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");


    public async Task<byte[]?> GenerateAsync(int avvikId)
    {
        var avvik = await db.Avvik
            .Include(a => a.Dor).ThenInclude(d => d!.Prosjekt)
            .FirstOrDefaultAsync(a => a.Id == avvikId);

        if (avvik is null)
        {
            return null;
        }

        var dor = avvik.Dor;
        var prosjektNavn = dor?.Prosjekt?.Navn ?? "";

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Avviksmelding", $"{prosjektNavn} · Dør {dor?.Dornummer}"));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 3,
                        ("Prosjekt", prosjektNavn), ("Dør", dor?.Dornummer),
                        ("Dato", avvik.OpprettetDato.ToString("dd.MM.yyyy")));

                    PdfStil.FriSeksjon(col, "Beskrivelse av avvik", avvik.Beskrivelse);
                    PdfStil.FriSeksjon(col, "Foreslått utbedring", avvik.UtbedringBeskrivelse);

                    if (avvik.Pris.HasValue)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Pris for utbedring");
                            inner.Item().PaddingTop(3).Text($"{avvik.Pris.Value.ToString("N2", Kultur)} kr eks. mva").FontSize(9);
                        });
                    }

                    col.Item().Column(inner =>
                    {
                        PdfStil.SeksjonTittel(inner, "Godkjenning");
                        if (avvik.Signatur is not null)
                        {
                            inner.Item().PaddingTop(4).Background(PdfStil.Sand).Border(1).BorderColor(PdfStil.SandBorder).Padding(6)
                                .Height(70).Width(200).Image(avvik.Signatur).FitArea();
                            inner.Item().PaddingTop(3).Text($"{avvik.SignertAvNavn} - signert {avvik.SignertDato?.ToString("dd.MM.yyyy HH:mm")}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            inner.Item().PaddingTop(4).Background(PdfStil.Sand).Border(1).BorderColor(PdfStil.SandBorder).Padding(6)
                                .Height(60).AlignMiddle().AlignCenter().Text("Signatur / navn / dato").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                        }
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
