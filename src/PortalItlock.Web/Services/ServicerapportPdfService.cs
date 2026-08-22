using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class ServicerapportPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

    public async Task<byte[]?> GenerateAsync(int servicerundeId)
    {
        var runde = await db.Servicerunder
            .Include(r => r.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(r => r.UtfortAvBruker)
            .Include(r => r.Deler).ThenInclude(d => d.Dor)
            .Include(r => r.Sjekkpunkter)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == servicerundeId);

        if (runde is null)
        {
            return null;
        }

        runde.Media = await db.ServicerundeMedia
            .Where(m => m.ServicerundeId == servicerundeId)
            .ToListAsync();

        var prosjekt = runde.Prosjekt;
        var prosjektNavn = prosjekt?.Navn ?? "";
        var kundeNavn = prosjekt?.Kunde?.Navn;

        var document = Document.Create(doc =>
        {
            if (!string.IsNullOrWhiteSpace(runde.Forside))
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
                            row.RelativeItem().AlignRight().Text($"Servicerapport – {prosjektNavn}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(14).Column(col => ForsideRenderer.Render(col, runde.Forside));
                });
            }

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Servicerapport").FontSize(20).SemiBold();
                        row.RelativeItem().AlignRight().Element(e => pdfLogo.Render(e, 16));
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Text(t =>
                    {
                        t.Span("Prosjekt: ").Bold();
                        t.Span(prosjektNavn);
                    });

                    if (!string.IsNullOrWhiteSpace(kundeNavn))
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Kunde: ").Bold();
                            t.Span(kundeNavn);
                        });
                    }

                    col.Item().Text(t =>
                    {
                        t.Span("Dato: ").Bold();
                        t.Span(runde.Dato.ToString("dd.MM.yyyy"));
                    });

                    if (runde.UtfortAvBruker is not null)
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Utført av: ").Bold();
                            t.Span(runde.UtfortAvBruker.Navn);
                        });
                    }

                    col.Item().PaddingTop(10).Text("Status og utført arbeid").Bold();
                    col.Item().Text(runde.StatusBeskrivelse);

                    if (!string.IsNullOrWhiteSpace(runde.Anbefalinger))
                    {
                        col.Item().PaddingTop(10).Text("Anbefalinger").Bold();
                        col.Item().Text(runde.Anbefalinger);
                    }

                    if (runde.NesteServiceDato.HasValue)
                    {
                        col.Item().PaddingTop(10).Text(t =>
                        {
                            t.Span("Neste planlagte service: ").Bold();
                            t.Span(runde.NesteServiceDato.Value.ToString("dd.MM.yyyy"));
                        });
                    }

                    if (runde.Deler.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Byttede deler").Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);
                                c.ConstantColumn(70);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Dato").SemiBold();
                                h.Cell().Text("Dør").SemiBold();
                                h.Cell().Text("Hva ble byttet").SemiBold();
                                h.Cell().Text("Hva var galt").SemiBold();
                            });

                            foreach (var del in runde.Deler.OrderBy(d => d.Dato))
                            {
                                table.Cell().Text(del.Dato.ToString("dd.MM.yyyy"));
                                table.Cell().Text(del.Dor?.Dornummer ?? "-");
                                table.Cell().Text(del.Beskrivelse);
                                table.Cell().Text(del.Feil ?? "-");
                            }
                        });
                    }

                    if (runde.Sjekkpunkter.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Sjekkliste").Bold();
                        col.Item().Column(sjekkCol =>
                        {
                            foreach (var punkt in runde.Sjekkpunkter.OrderBy(p => p.Rekkefolge))
                            {
                                sjekkCol.Item().Row(row =>
                                {
                                    row.ConstantItem(16).Text(punkt.Fullfort ? "[x]" : "[ ]");
                                    row.RelativeItem().Text(punkt.Tekst);
                                });
                            }
                        });
                    }

                    if (runde.Media.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Bilder").Bold();
                        foreach (var bilde in runde.Media)
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
