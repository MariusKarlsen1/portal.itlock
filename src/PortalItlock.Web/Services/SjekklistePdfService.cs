using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class SjekklistePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{

    public async Task<byte[]?> GenerateAsync(int arbeidsordreId)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Prosjekt)
            .Include(a => a.AnsvarligMontor)
            .FirstOrDefaultAsync(a => a.Id == arbeidsordreId);

        if (ordre is null)
        {
            return null;
        }

        var punkter = await db.ArbeidsordreSjekkpunkter
            .Include(p => p.FullfortAvBruker)
            .Where(p => p.ArbeidsordreId == arbeidsordreId)
            .OrderBy(p => p.Rekkefolge)
            .ToListAsync();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Sjekkliste", ordre.Tittel));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 3,
                        ("Prosjekt", ordre.Prosjekt?.Navn), ("Ansvarlig montør", ordre.AnsvarligMontor?.Navn),
                        ("Dato", DateTime.Now.ToString("dd.MM.yyyy")));

                    if (punkter.Count == 0)
                    {
                        col.Item().Text("Ingen sjekklistepunkter registrert.").FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        var fullfort = punkter.Count(p => p.Fullfort);
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, $"{fullfort} av {punkter.Count} punkter fullført");
                            inner.Item().PaddingTop(4).Column(sjekkCol =>
                            {
                                sjekkCol.Spacing(4);
                                foreach (var p in punkter)
                                {
                                    var underTekst = p.Fullfort && p.FullfortDato.HasValue
                                        ? $"Fullført {p.FullfortDato.Value.ToString("dd.MM.yyyy HH:mm")}{(p.FullfortAvBruker is not null ? $" av {p.FullfortAvBruker.Navn}" : "")}"
                                        : null;
                                    PdfStil.SjekkRad(sjekkCol, p.Fullfort, p.Tekst, underTekst);
                                }
                            });
                        });
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
