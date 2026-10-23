using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Priskodene for organisasjonen (arbeid, kjøring, småmateriell osv.).
public class PrisKodeService(ApplicationDbContext db)
{
    public async Task<List<PrisKode>> AlleAsync(bool bareAktive = false) =>
        await db.PrisKoder
            .Where(k => !bareAktive || k.Aktiv)
            .OrderBy(k => k.Rekkefolge).ThenBy(k => k.Kode)
            .ToListAsync();

    // En ny organisasjon får standardkodene første gang de trengs. Timekosten
    // hentes fra Prosjektøkonomi, og timeprisen fra det siste tilbudet, så
    // prisene ligner det organisasjonen allerede bruker.
    public async Task<List<PrisKode>> SikreStandardAsync()
    {
        if (await db.PrisKoder.AnyAsync())
        {
            return await AlleAsync();
        }

        var intern = (await db.OkonomiInnstillinger.AsNoTracking().OrderBy(i => i.Id).FirstOrDefaultAsync())?.InternTimekost;
        var timekost = intern is > 0 ? intern.Value : 550m;
        var timepris = await db.Tilbud.AsNoTracking().Where(t => t.Timepris > 0).OrderByDescending(t => t.Id).Select(t => (decimal?)t.Timepris).FirstOrDefaultAsync() ?? 950m;
        db.PrisKoder.AddRange(KodeLinjeBeregner.Standardkoder(timekost, timepris));
        await db.SaveChangesAsync();
        return await AlleAsync();
    }

    public static string NesteKode(IEnumerable<PrisKode> koder)
    {
        var tall = koder.Select(k => int.TryParse(k.Kode, out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return (tall < 100 ? 100 : (tall / 10 + 1) * 10).ToString();
    }
}
