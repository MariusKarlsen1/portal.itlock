using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

public class PrisendringUtforerService(ApplicationDbContext db, ILogger<PrisendringUtforerService> logger)
{
    public async Task<int> UtforForfalteAsync()
    {
        var idag = DateTime.Today;
        var forfalte = await db.PlanlagtePrisendringer
            .Include(p => p.Component)
            .Where(p => !p.Utfort && p.GjelderFraDato <= idag)
            .ToListAsync();

        foreach (var p in forfalte)
        {
            if (p.Component is null)
            {
                p.Utfort = true;
                continue;
            }

            PrisHistorikkLogger.Logg(db, p.Component, p.NyPrisNetto, p.NyPrisVeiledende, "Planlagt prisoppdatering");
            p.Component.PrisNetto = p.NyPrisNetto;
            p.Component.PrisVeiledende = p.NyPrisVeiledende;
            p.Utfort = true;
        }

        if (forfalte.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Utførte {Antall} planlagte prisendring(er).", forfalte.Count);
        }

        return forfalte.Count;
    }
}
