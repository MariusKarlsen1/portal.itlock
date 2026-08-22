using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class ArbeidsordrePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

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
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Arbeidsordrerapport").FontSize(20).SemiBold();
                        row.RelativeItem().AlignRight().Element(e => pdfLogo.Render(e, 16));
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Text(t =>
                    {
                        t.Span("Tittel: ").Bold();
                        t.Span(ordre.Tittel);
                    });

                    if (!string.IsNullOrWhiteSpace(prosjektNavn))
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Prosjekt: ").Bold();
                            t.Span(prosjektNavn);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(kundeNavn))
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Kunde: ").Bold();
                            t.Span(kundeNavn);
                        });
                    }

                    if (ordre.PlanlagtDato.HasValue)
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Dato: ").Bold();
                            t.Span(ordre.PlanlagtDato.Value.ToString("dd.MM.yyyy"));
                        });
                    }

                    if (ordre.AnsvarligMontor is not null)
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Utført av: ").Bold();
                            t.Span(ordre.AnsvarligMontor.Navn);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(ordre.Beskrivelse))
                    {
                        col.Item().PaddingTop(10).Text("Beskrivelse av jobben").Bold();
                        col.Item().Text(ordre.Beskrivelse);
                    }

                    if (!string.IsNullOrWhiteSpace(ordre.UtfortArbeid))
                    {
                        col.Item().PaddingTop(10).Text("Hva er gjort").Bold();
                        col.Item().Text(ordre.UtfortArbeid);
                    }

                    if (!string.IsNullOrWhiteSpace(ordre.Anbefalinger))
                    {
                        col.Item().PaddingTop(10).Text("Hva var galt / anbefalinger").Bold();
                        col.Item().Text(ordre.Anbefalinger);
                    }

                    if (ordre.Sjekkpunkter.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Sjekkliste").Bold();
                        col.Item().Column(sjekkCol =>
                        {
                            foreach (var punkt in ordre.Sjekkpunkter.OrderBy(p => p.Rekkefolge))
                            {
                                sjekkCol.Item().Row(row =>
                                {
                                    row.ConstantItem(16).Text(punkt.Fullfort ? "[x]" : "[ ]");
                                    row.RelativeItem().Text(punkt.Tekst);
                                });
                            }
                        });
                    }

                    if (ordre.Media.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Bilder").Bold();
                        foreach (var bilde in ordre.Media)
                        {
                            col.Item().PaddingTop(6).Width(220).Image(PdfBilde.Forminsk(bilde.Data));
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
