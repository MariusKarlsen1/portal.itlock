using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public class PackageMatchingService(ApplicationDbContext db)
{
    // Strict match: for every requirement dimension with at least one checked
    // value, the package must satisfy one of the checked values in that
    // dimension. Dimensions with nothing checked are ignored.
    //
    // Treffene regnes ut på (pakke, kravverdi)-parene alene. Tidligere lastet
    // metoden hver eneste pakke med alle krav OG alle komponenter med
    // komponenttype - hver gang man huket av en boks. Det er det tunge her, og
    // det er det som gjorde siden treg å trykke på.
    public async Task<List<Package>> FindMatchingPackagesAsync(IReadOnlyCollection<int> selectedRequirementValueIds)
    {
        if (selectedRequirementValueIds.Count == 0)
        {
            return [];
        }

        var selectedValues = await db.RequirementValues
            .Where(v => selectedRequirementValueIds.Contains(v.Id))
            .Select(v => new { v.Id, v.RequirementDimensionId })
            .ToListAsync();

        var dimensionGroups = selectedValues
            .GroupBy(v => v.RequirementDimensionId)
            .Select(g => g.Select(v => v.Id).ToHashSet())
            .ToList();

        if (dimensionGroups.Count == 0)
        {
            return [];
        }

        // To kolonner for de kravene som faktisk er huket av - ingen pakker,
        // ingen komponenter.
        var kravPar = await db.PackageRequirements
            .Where(k => selectedRequirementValueIds.Contains(k.RequirementValueId))
            .Select(k => new { k.PackageId, k.RequirementValueId })
            .ToListAsync();

        var treffIder = kravPar
            .GroupBy(k => k.PackageId)
            .Where(g => dimensionGroups.All(gruppe => g.Any(k => gruppe.Contains(k.RequirementValueId))))
            .Select(g => g.Key)
            .ToList();

        if (treffIder.Count == 0)
        {
            return [];
        }

        // Resultatlista viser navn og kravmerker. Komponentene hentes først når
        // man åpner en pakke - se HentPakkeMedKomponenterAsync.
        return await db.Packages
            .AsNoTracking()
            .Where(p => treffIder.Contains(p.Id))
            .Include(p => p.Krav)
                .ThenInclude(k => k.RequirementValue)
                    .ThenInclude(v => v!.Dimensjon)
            .OrderBy(p => p.Navn)
            .AsSplitQuery()
            .ToListAsync();
    }

    // Detaljpanelet trenger komponentene med type og priser. Det er bare én
    // pakke av gangen, så den kan lastes tungt uten at det merkes.
    public async Task<Package?> HentPakkeMedKomponenterAsync(int pakkeId) =>
        await db.Packages
            .AsNoTracking()
            .Include(p => p.Krav)
                .ThenInclude(k => k.RequirementValue)
                    .ThenInclude(v => v!.Dimensjon)
            .Include(p => p.Komponenter)
                .ThenInclude(pc => pc.Component)
                    .ThenInclude(c => c!.Type)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == pakkeId);
}
