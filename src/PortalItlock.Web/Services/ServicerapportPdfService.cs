using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class ServicerapportPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{

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
                    page.Margin(1.8f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                    page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Servicerapport", prosjektNavn));

                    page.Content().PaddingTop(14).Column(col => ForsideRenderer.Render(col, runde.Forside));
                });
            }

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Servicerapport", prosjektNavn));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 3,
                        ("Prosjekt", prosjektNavn), ("Kunde", kundeNavn),
                        ("Dato", runde.Dato.ToString("dd.MM.yyyy")),
                        ("Utført av", runde.UtfortAvBruker?.Navn));

                    PdfStil.FriSeksjon(col, "Status og utført arbeid", runde.StatusBeskrivelse);
                    PdfStil.FriSeksjon(col, "Anbefalinger", runde.Anbefalinger);

                    if (runde.NesteServiceDato.HasValue)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Neste planlagte service");
                            inner.Item().PaddingTop(3).Text(runde.NesteServiceDato.Value.ToString("dd.MM.yyyy")).FontSize(9);
                        });
                    }

                    if (runde.Deler.Count > 0)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Byttede deler");
                            inner.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(65);
                                    c.ConstantColumn(60);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                });

                                table.Header(h =>
                                {
                                    PdfStil.TabellHode(h.Cell()).Text("Dato").FontSize(8).Bold().FontColor(Colors.White);
                                    PdfStil.TabellHode(h.Cell()).Text("Dør").FontSize(8).Bold().FontColor(Colors.White);
                                    PdfStil.TabellHode(h.Cell()).Text("Hva ble byttet").FontSize(8).Bold().FontColor(Colors.White);
                                    PdfStil.TabellHode(h.Cell()).Text("Hva var galt").FontSize(8).Bold().FontColor(Colors.White);
                                });

                                var i = 0;
                                foreach (var del in runde.Deler.OrderBy(d => d.Dato))
                                {
                                    var alt = i++ % 2 == 1;
                                    PdfStil.TabellRad(table.Cell(), alt).Text(del.Dato.ToString("dd.MM.yyyy")).FontSize(8.5f);
                                    PdfStil.TabellRad(table.Cell(), alt).Text(del.Dor?.Dornummer ?? "-").FontSize(8.5f);
                                    PdfStil.TabellRad(table.Cell(), alt).Text(del.Beskrivelse).FontSize(8.5f);
                                    PdfStil.TabellRad(table.Cell(), alt).Text(del.Feil ?? "-").FontSize(8.5f);
                                }
                            });
                        });
                    }

                    if (runde.Sjekkpunkter.Count > 0)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Sjekkliste");
                            inner.Item().PaddingTop(3).Column(sjekkCol =>
                            {
                                sjekkCol.Spacing(3);
                                foreach (var punkt in runde.Sjekkpunkter.OrderBy(p => p.Rekkefolge))
                                {
                                    PdfStil.SjekkRad(sjekkCol, punkt.Fullfort, punkt.Tekst);
                                }
                            });
                        });
                    }

                    if (runde.Media.Count > 0)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Bilder");
                            foreach (var chunk in runde.Media.Chunk(3))
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
