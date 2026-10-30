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
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, $"Sak #{ticket.Id}", ticket.Tittel));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 3,
                        ("Kunde", ticket.Kunde?.Navn), ("Lokasjon", ticket.Lokasjon),
                        ("Opprettet", ticket.OpprettetDato.ToString("dd.MM.yyyy")),
                        ("Lukket", ticket.LukketDato?.ToString("dd.MM.yyyy")),
                        ("Ansvarlig", ticket.AnsvarligBruker?.Navn));

                    PdfStil.FriSeksjon(col, "Beskrivelse", ticket.Beskrivelse);
                    PdfStil.FriSeksjon(col, "Oppsummering", ticket.Sluttoppsummering);
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
