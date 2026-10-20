using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Kjører en bakgrunnsjobb for hver aktive organisasjon, hver i sitt eget
// DI-scope der ITenantContext er satt til organisasjonen. Da peker
// ApplicationDbContext, TilleggsmodulService osv. i scopet på riktig
// organisasjons database, akkurat som i en vanlig forespørsel.
//
// Før dette kjørte bakgrunnsjobbene i et scope uten forespørsel, og
// TenantContext falt da alltid tilbake til standard-organisasjonen (itlock),
// så de andre organisasjonene fikk aldri påminnelser.
public class OrganisasjonKjorer(IServiceScopeFactory scopeFactory, ILogger<OrganisasjonKjorer> logger)
{
    public async Task ForHverAsync(string jobb, Func<IServiceProvider, Tenant, Task> arbeid, CancellationToken ct = default)
    {
        List<Tenant> organisasjoner;
        using (var scope = scopeFactory.CreateScope())
        {
            var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            organisasjoner = await platformDb.Tenants.AsNoTracking()
                .Where(t => t.Status == TenantStatus.Aktiv)
                .OrderBy(t => t.Id)
                .ToListAsync(ct);
        }

        foreach (var tenant in organisasjoner)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<ITenantContext>().Bruk(tenant);
                await arbeid(scope.ServiceProvider, tenant);
            }
            catch (Exception ex)
            {
                // Én organisasjon som feiler skal ikke stoppe de andre.
                logger.LogError(ex, "{Jobb} feilet for {Organisasjon}.", jobb, tenant.Navn);
            }
        }
    }
}
