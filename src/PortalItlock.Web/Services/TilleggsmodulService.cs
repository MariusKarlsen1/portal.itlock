using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Hvilke tilleggsmoduler (TilleggsModuler) organisasjonen har fått slått på
// av plattformeier. Leses fra PlatformDbContext og caches kort per
// organisasjon, siden menyen spør ved hver sidenavigasjon.
public class TilleggsmodulService(PlatformDbContext platformDb, ITenantContext tenantCtx, IMemoryCache cache)
{
    private static readonly TimeSpan CacheLevetid = TimeSpan.FromSeconds(60);

    public static string CacheNokkel(int tenantId) => $"tilleggsmoduler:{tenantId}";

    // Tømmes fra plattform-admin når en modul slås av/på, så endringen
    // gjelder med en gang i stedet for etter at cachen går ut.
    public static void Nullstill(IMemoryCache cache, int tenantId) => cache.Remove(CacheNokkel(tenantId));

    public async Task<IReadOnlySet<string>> AktiveAsync()
    {
        var tenantId = tenantCtx.Current?.Id;
        if (tenantId is null)
        {
            return new HashSet<string>();
        }

        if (cache.TryGetValue(CacheNokkel(tenantId.Value), out IReadOnlySet<string>? cachet) && cachet is not null)
        {
            return cachet;
        }

        var aktive = (await platformDb.TenantModuler
                .AsNoTracking()
                .Where(m => m.TenantId == tenantId.Value && m.Aktiv)
                .Select(m => m.Modul)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        cache.Set(CacheNokkel(tenantId.Value), (IReadOnlySet<string>)aktive, CacheLevetid);
        return aktive;
    }

    public async Task<bool> HarAsync(string modul) => (await AktiveAsync()).Contains(modul);
}
