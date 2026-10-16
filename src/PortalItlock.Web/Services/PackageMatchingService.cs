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

    public sealed record NestenTreff(Package Pakke, string ManglerDimensjon, int Oppfylt, int Totalt);

    // "Nesten"-treff (D2): pakker som oppfyller alle valgte kravgrupper unntatt
    // én. Gir bare mening når minst to kravgrupper er valgt.
    public async Task<List<NestenTreff>> FinnNestenTreffAsync(IReadOnlyCollection<int> selectedRequirementValueIds)
    {
        if (selectedRequirementValueIds.Count == 0)
        {
            return [];
        }

        var selectedValues = await db.RequirementValues
            .Where(v => selectedRequirementValueIds.Contains(v.Id))
            .Select(v => new { v.Id, v.RequirementDimensionId, DimensjonNavn = v.Dimensjon!.Navn })
            .ToListAsync();

        var grupper = selectedValues
            .GroupBy(v => new { v.RequirementDimensionId, v.DimensjonNavn })
            .Select(g => (Navn: g.Key.DimensjonNavn, Verdier: g.Select(v => v.Id).ToHashSet()))
            .ToList();

        if (grupper.Count < 2)
        {
            return [];
        }

        var kravPar = await db.PackageRequirements
            .Where(k => selectedRequirementValueIds.Contains(k.RequirementValueId))
            .Select(k => new { k.PackageId, k.RequirementValueId })
            .ToListAsync();

        var mangler = kravPar
            .GroupBy(k => k.PackageId)
            .Select(g => new
            {
                PakkeId = g.Key,
                Manglende = grupper.Where(gr => !g.Any(k => gr.Verdier.Contains(k.RequirementValueId))).Select(gr => gr.Navn).ToList()
            })
            .Where(x => x.Manglende.Count == 1)
            .ToDictionary(x => x.PakkeId, x => x.Manglende[0]);

        if (mangler.Count == 0)
        {
            return [];
        }

        var pakker = await db.Packages
            .AsNoTracking()
            .Where(p => mangler.Keys.Contains(p.Id))
            .Include(p => p.Krav)
                .ThenInclude(k => k.RequirementValue)
                    .ThenInclude(v => v!.Dimensjon)
            .OrderBy(p => p.Navn)
            .AsSplitQuery()
            .ToListAsync();

        return pakker.Select(p => new NestenTreff(p, mangler[p.Id], grupper.Count - 1, grupper.Count)).ToList();
    }

    // Antall komponenter og sum veiledende pris per pakke, til resultatlista.
    public async Task<Dictionary<int, (int Antall, decimal Sum)>> HentSammendragAsync(IReadOnlyCollection<int> pakkeIder)
    {
        if (pakkeIder.Count == 0)
        {
            return [];
        }

        var rader = await db.PackageComponents
            .Where(pc => pakkeIder.Contains(pc.PackageId))
            .Select(pc => new { pc.PackageId, pc.Antall, Pris = pc.Component!.PrisVeiledende })
            .ToListAsync();

        return rader
            .GroupBy(r => r.PackageId)
            .ToDictionary(g => g.Key, g => (g.Count(), g.Sum(r => (r.Pris ?? 0) * r.Antall)));
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
