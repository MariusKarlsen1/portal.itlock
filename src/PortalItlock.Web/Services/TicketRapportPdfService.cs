using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class TicketRapportPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<byte[]?> GenerateAsync(int ticketId)
    {
        var ticket = await db.Tickets
            .Include(t => t.Kunde)
            .Include(t => t.AnsvarligBruker)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null)
        {
            return null;
        }

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
                        row.RelativeItem().Element(e => pdfLogo.Render(e, 15));
                        row.RelativeItem().AlignRight().Text($"Sak #{ticket.Id} - oppsummering").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Text(ticket.Tittel).FontSize(16).Bold();
                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Kunde: {ticket.Kunde?.Navn ?? "-"}");
                        row.RelativeItem().Text($"Lokasjon: {ticket.Lokasjon ?? "-"}");
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Opprettet: {ticket.OpprettetDato:dd.MM.yyyy}");
                        row.RelativeItem().Text($"Lukket: {(ticket.LukketDato.HasValue ? ticket.LukketDato.Value.ToString("dd.MM.yyyy") : "-")}");
                    });
                    col.Item().Text($"Ansvarlig: {ticket.AnsvarligBruker?.Navn ?? "-"}");

                    if (!string.IsNullOrWhiteSpace(ticket.Beskrivelse))
                    {
                        col.Item().PaddingTop(12).Text("Beskrivelse").Bold();
                        col.Item().Text(ticket.Beskrivelse);
                    }

                    if (!string.IsNullOrWhiteSpace(ticket.Sluttoppsummering))
                    {
                        col.Item().PaddingTop(12).Text("Oppsummering").Bold();
                        col.Item().Text(ticket.Sluttoppsummering);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
