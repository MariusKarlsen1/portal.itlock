using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Hvem i gjeldende organisasjon som skal ha en påminnelse. Settes opp av
// plattformeier under «E-post og varsler» i adminportalen. Er ingen satt opp,
// går påminnelsen til organisasjonens aktive administratorer, så ingenting
// forsvinner før oppsettet er gjort.
public class VarselMottakerService(PlatformDbContext platformDb, ApplicationDbContext db, ITenantContext tenantContext)
{
    public async Task<List<string>> HentAsync(VarselType type)
    {
        if (tenantContext.Current is not { } tenant)
        {
            return [];
        }

        var mottakere = await platformDb.TenantVarselMottakere.AsNoTracking()
            .Where(m => m.TenantId == tenant.Id)
            .ToListAsync();

        if (mottakere.Count > 0)
        {
            return mottakere.Where(m => m.Gjelder(type))
                .Select(m => m.Epost.Trim())
                .Where(e => e.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return await db.Brukere.AsNoTracking()
            .Where(b => b.Rolle == BrukerRolle.Admin && b.Aktiv && b.Epost != "")
            .Select(b => b.Epost)
            .Distinct()
            .ToListAsync();
    }

    // Sender samme e-post til alle mottakerne. Gir true hvis minst én fikk den.
    public async Task<bool> SendAsync(EmailService epost, VarselType type, string emne, string html, ILogger logger)
    {
        var mottakere = await HentAsync(type);
        if (mottakere.Count == 0)
        {
            logger.LogInformation("Ingen mottakere for {Type} i {Organisasjon}.", type, tenantContext.Current?.Navn);
            return false;
        }

        var noenSendt = false;
        foreach (var mottaker in mottakere)
        {
            if (await epost.SendAsync(mottaker, emne, html))
            {
                noenSendt = true;
            }
            else
            {
                logger.LogWarning("Klarte ikke å sende {Type}-varsel til {Mottaker}.", type, mottaker);
            }
        }

        return noenSendt;
    }
}
