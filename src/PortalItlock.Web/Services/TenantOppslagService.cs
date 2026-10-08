using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Finner hvilken organisasjon en e-postadresse hører til - uansett hvilket
// vertsnavn forespørselen faktisk kom inn på. Brukes av alle
// konto-endepunktene (innlogging, glemt passord, sett passord), slik at de
// virker likt enten organisasjonen har sitt eget domene, eller alle deler
// samme adresse.
public class TenantOppslagService(PlatformDbContext platformDb, ITenantContext tenantCtx)
{
    public async Task<Tenant?> FinnTenantForEpostAsync(string epost)
    {
        var epostLower = epost.Trim().ToLowerInvariant();
        if (epostLower.Length == 0)
        {
            return null;
        }

        // Rask vei: organisasjonen vertsnavnet allerede peker til (vanligst,
        // og unngår å måtte åpne N databaser for det vanlige tilfellet).
        var navarende = tenantCtx.Current;
        if (navarende is not null && await FinnesIAsync(navarende, epostLower))
        {
            return navarende;
        }

        var aktive = await platformDb.Tenants
            .Where(t => t.Status == TenantStatus.Aktiv)
            .ToListAsync();

        foreach (var tenant in aktive)
        {
            if (tenant.Id == navarende?.Id)
            {
                continue;
            }

            if (await FinnesIAsync(tenant, epostLower))
            {
                return tenant;
            }
        }

        return null;
    }

    private static async Task<bool> FinnesIAsync(Tenant tenant, string epost)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(tenant.ConnectionString)
            .Options;
        await using var db = new ApplicationDbContext(options);
        var eposter = await db.Brukere.Select(b => b.Epost).ToListAsync();
        return eposter.Any(e => EpostHjelper.ErLik(e, epost));
    }
}
