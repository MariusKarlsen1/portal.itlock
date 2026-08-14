using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public class PackageMatchingService(ApplicationDbContext db)
{
    // Strict match: for every requirement dimension with at least one checked
    // value, the package must satisfy one of the checked values in that
    // dimension. Dimensions with nothing checked are ignored.
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

        var candidates = await db.Packages
            .Include(p => p.Krav)
                .ThenInclude(k => k.RequirementValue)
                    .ThenInclude(v => v!.Dimensjon)
            .Include(p => p.Komponenter)
                .ThenInclude(pc => pc.Component)
                    .ThenInclude(c => c!.Type)
            .ToListAsync();

        return candidates
            .Where(p => dimensionGroups.All(group =>
                p.Krav.Any(k => group.Contains(k.RequirementValueId))))
            .OrderBy(p => p.Navn)
            .ToList();
    }
}
