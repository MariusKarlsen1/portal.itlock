using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
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
            .Include(a => a.AnsvarligBruker)
            .Include(a => a.Media)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == avvikId);

        if (avvik is null)
        {
            return null;
        }

        var dor = avvik.Dor;
        var prosjektNavn = dor?.Prosjekt?.Navn ?? "";
        var bilder = avvik.Media.Where(m => m.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Id).ToList();

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
                        ("Avvik", avvik.NummerTekst), ("Prosjekt", prosjektNavn), ("Dør", dor?.Dornummer),
                        ("Dato", avvik.OpprettetDato.ToString("dd.MM.yyyy")), ("Prioritet", avvik.Prioritet.Visningsnavn()),
                        ("Ansvarlig", avvik.AnsvarligBruker?.Navn ?? "-"));

                    if (!string.IsNullOrWhiteSpace(avvik.Tittel))
                    {
                        col.Item().Text(avvik.Tittel).FontSize(13).SemiBold();
                    }

                    PdfStil.FriSeksjon(col, "Beskrivelse av avvik", avvik.Beskrivelse);

                    // Bildene to og to, rotert slik de er satt på avvikssiden.
                    if (bilder.Count > 0)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Bilder");
                            foreach (var par in bilder.Chunk(2))
                            {
                                inner.Item().PaddingTop(6).Row(row =>
                                {
                                    row.Spacing(8);
                                    foreach (var b in par)
                                    {
                                        var celle = row.RelativeItem().Height(190).Border(1).BorderColor(PdfStil.SandBorder).Padding(3).AlignCenter().AlignMiddle();
                                        var rotert = b.Rotasjon switch
                                        {
                                            90 => celle.RotateRight(),
                                            180 => celle.RotateRight().RotateRight(),
                                            270 => celle.RotateLeft(),
                                            _ => celle
                                        };
                                        rotert.Image(b.Data).FitArea();
                                    }

                                    if (par.Length == 1)
                                    {
                                        row.RelativeItem();
                                    }
                                });
                            }
                        });
                    }

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
