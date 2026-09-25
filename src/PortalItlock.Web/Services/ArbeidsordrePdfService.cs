using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class ArbeidsordrePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{

    public async Task<byte[]?> GenerateAsync(int arbeidsordreId)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.AnsvarligMontor)
            .Include(a => a.Sjekkpunkter)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == arbeidsordreId);

        if (ordre is null)
        {
            return null;
        }

        ordre.Media = await db.ArbeidsordreMedia
            .Where(m => m.ArbeidsordreId == arbeidsordreId)
            .ToListAsync();

        var prosjektNavn = ordre.Prosjekt?.Navn;
        var kundeNavn = ordre.Prosjekt?.Kunde?.Navn;

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Arbeidsordrerapport", ordre.Tittel));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 3,
                        ("Prosjekt", prosjektNavn), ("Kunde", kundeNavn),
                        ("Dato", ordre.PlanlagtDato?.ToString("dd.MM.yyyy")),
                        ("Utført av", ordre.AnsvarligMontor?.Navn));

                    PdfStil.FriSeksjon(col, "Beskrivelse av jobben", ordre.Beskrivelse);
                    PdfStil.FriSeksjon(col, "Hva er gjort", ordre.UtfortArbeid);
                    PdfStil.FriSeksjon(col, "Hva var galt / anbefalinger", ordre.Anbefalinger);

                    if (ordre.Sjekkpunkter.Count > 0)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Sjekkliste");
                            inner.Item().PaddingTop(3).Column(sjekkCol =>
                            {
                                sjekkCol.Spacing(3);
                                foreach (var punkt in ordre.Sjekkpunkter.OrderBy(p => p.Rekkefolge))
                                {
                                    PdfStil.SjekkRad(sjekkCol, punkt.Fullfort, punkt.Tekst);
                                }
                            });
                        });
                    }

                    if (ordre.Media.Count > 0)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Bilder");
                            foreach (var chunk in ordre.Media.Chunk(3))
                            {
                                inner.Item().PaddingTop(4).Row(row =>
                                {
                                    foreach (var bilde in chunk)
                                    {
                                        row.RelativeItem().Padding(2).Border(1).BorderColor(PdfStil.SandBorder).Height(140)
                                            .Image(PdfBilde.Forminsk(bilde.Data)).FitArea();
                                    }
                                });
                            }
                        });
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
