using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// Tilbud som er godkjent eller avslått uten å ha vært «Sendt til kunde» i
/// portalen (f.eks. sendt på e-post) manglet sendt-dato og frosset sum, og
/// telte derfor ikke med i alle rapportene. Her får de dato (svardatoen) og
/// sum (regnet eks. mva som på tilbudet) lagret én gang, så Dashbord,
/// Tilbudsreserve, kundekortet og rapportene viser de samme tallene.
/// TilbudSkjema gjør det samme når status settes, så dette gjelder eldre tilbud.
public static class TilbudSendtFiks
{
    public static async Task SikreAsync(ApplicationDbContext db)
    {
        var mangler = await db.Tilbud
            .Where(t => (t.Status == EndringsmeldingStatus.Godkjent || t.Status == EndringsmeldingStatus.Avslatt)
                && (t.SendtDato == null || t.SendtSum == null))
            .Include(t => t.Linjer)
            .Include(t => t.KodeLinjer)
            .AsSplitQuery()
            .ToListAsync();
        if (mangler.Count == 0)
        {
            return;
        }

        foreach (var t in mangler)
        {
            t.SendtDato ??= t.GodkjentDato ?? t.AvslattDato?.ToLocalTime().Date ?? t.OpprettetDato.ToLocalTime().Date;
            t.SendtSum ??= TilbudBeregningHelper.BeregnTotaltUtenMva(t);
        }
        await db.SaveChangesAsync();
    }
}
