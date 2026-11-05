using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Vedlegg på en forespørsel følger med når forespørselen kobles til en ticket.
// Innebygde bilder, som logoen i en e-postsignatur, er ikke ekte vedlegg og hoppes over.
public static class TicketVedleggKopi
{
    public static async Task<int> KopierFraForesporselAsync(ApplicationDbContext db, int foresporselId, int ticketId)
    {
        var finnes = await db.TicketMedia
            .Where(m => m.TicketId == ticketId)
            .Select(m => m.Filnavn)
            .ToListAsync();

        var vedlegg = await db.ForesporselMedia
            .Where(m => m.ForesporselId == foresporselId && !m.ErInline)
            .ToListAsync();

        var nye = vedlegg.Where(v => !finnes.Contains(v.Filnavn)).ToList();
        foreach (var v in nye)
        {
            db.TicketMedia.Add(new TicketMedia
            {
                TicketId = ticketId,
                Data = v.Data,
                ContentType = v.ContentType,
                Filnavn = v.Filnavn
            });
        }

        return nye.Count;
    }
}
