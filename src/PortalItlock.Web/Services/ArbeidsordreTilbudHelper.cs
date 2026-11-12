using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

/// Alle tilbudene på en arbeidsordre: hovedtilbudet (Arbeidsordre.TilbudId)
/// først, deretter de ekstra (ArbeidsordreTilbud) i rekkefølgen de ble knyttet.
public static class ArbeidsordreTilbudHelper
{
    public static async Task<List<int>> TilbudIderAsync(ApplicationDbContext db, int arbeidsordreId, int? hovedTilbudId)
    {
        var ekstra = await db.ArbeidsordreTilbud.AsNoTracking()
            .Where(x => x.ArbeidsordreId == arbeidsordreId)
            .OrderBy(x => x.Id)
            .Select(x => x.TilbudId)
            .ToListAsync();
        return (hovedTilbudId is { } h ? [h] : Array.Empty<int>()).Concat(ekstra).Distinct().ToList();
    }

    /// Arbeidsordre-id -> tilbudene på den, for mange arbeidsordre på én gang.
    public static async Task<Dictionary<int, List<int>>> TilbudIderAsync(ApplicationDbContext db, IEnumerable<(int Id, int? TilbudId)> ordre)
    {
        var liste = ordre.ToList();
        var ider = liste.Select(o => o.Id).ToList();
        var ekstra = await db.ArbeidsordreTilbud.AsNoTracking()
            .Where(x => ider.Contains(x.ArbeidsordreId))
            .OrderBy(x => x.Id)
            .Select(x => new { x.ArbeidsordreId, x.TilbudId })
            .ToListAsync();
        return liste.ToDictionary(o => o.Id, o =>
            (o.TilbudId is { } h ? [h] : Array.Empty<int>())
                .Concat(ekstra.Where(e => e.ArbeidsordreId == o.Id).Select(e => e.TilbudId))
                .Distinct().ToList());
    }
}
