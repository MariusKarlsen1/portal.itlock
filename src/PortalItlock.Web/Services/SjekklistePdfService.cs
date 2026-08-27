using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class SjekklistePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

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
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Sjekkliste").FontSize(18).SemiBold();
                        row.RelativeItem().AlignRight().Element(e => pdfLogo.Render(e, 16));
                    });
                    col.Item().PaddingTop(2).Text(ordre.Tittel).FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Text(t =>
                    {
                        t.Span("Prosjekt: ").Bold();
                        t.Span(ordre.Prosjekt?.Navn ?? "-");
                    });
                    if (ordre.AnsvarligMontor is not null)
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Ansvarlig montør: ").Bold();
                            t.Span(ordre.AnsvarligMontor.Navn);
                        });
                    }
                    col.Item().Text(t =>
                    {
                        t.Span("Dato: ").Bold();
                        t.Span(DateTime.Now.ToString("dd.MM.yyyy"));
                    });

                    col.Item().PaddingTop(10);

                    if (punkter.Count == 0)
                    {
                        col.Item().Text("Ingen sjekklistepunkter registrert.").FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        var fullfort = punkter.Count(p => p.Fullfort);
                        col.Item().PaddingBottom(6).Text($"{fullfort} av {punkter.Count} punkter fullført").SemiBold();

                        foreach (var p in punkter)
                        {
                            col.Item().Row(row =>
                            {
                                row.ConstantItem(18).Text(p.Fullfort ? "☑" : "☐").FontSize(12);
                                row.RelativeItem().Column(pc =>
                                {
                                    pc.Item().Text(p.Tekst);
                                    if (p.Fullfort && p.FullfortDato.HasValue)
                                    {
                                        pc.Item().Text($"Fullført {p.FullfortDato.Value.ToString("dd.MM.yyyy HH:mm")}{(p.FullfortAvBruker is not null ? $" av {p.FullfortAvBruker.Navn}" : "")}")
                                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                                    }
                                });
                            });
                        }
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Column(c =>
                {
                    c.Item().AlignCenter().Text($"{FirmaNavn} - {FirmaAdresse} - Tlf {FirmaTelefon} - {FirmaEpost}").FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }
}
