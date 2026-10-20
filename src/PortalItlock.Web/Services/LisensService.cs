using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Lisensene til gjeldende organisasjon: hvor mange som er tildelt (satt i
// adminportalen) og hvor mange som er i bruk (aktive brukere med rollen).
// Brukes når brukere opprettes eller får ny rolle.
public class LisensService(ApplicationDbContext db, PlatformDbContext platformDb, ITenantContext tenantContext)
{
    public sealed record Status(LisensType Type, int Tildelt, int Brukt)
    {
        public bool Full => Tildelt > 0 && Brukt >= Tildelt;
        public int Ledige => Math.Max(0, Tildelt - Brukt);
    }

    public async Task<List<Status>> AlleAsync()
    {
        var tildelt = await TildeltAsync();
        var brukt = await db.Brukere.AsNoTracking().Where(b => b.Aktiv)
            .GroupBy(b => b.Rolle).Select(g => new { g.Key, Antall = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Antall);

        return LisensTypeExtensions.Aktive
            .Select(t => new Status(t, tildelt.GetValueOrDefault(t), t.Rolle() is { } r ? brukt.GetValueOrDefault(r) : 0))
            .ToList();
    }

    // Feilmelding hvis det ikke er ledig lisens for rollen, ellers null.
    // Tildelt 0 betyr ingen grense (som før), så organisasjoner uten
    // oppsatte lisenser ikke blir stengt ute.
    public async Task<string?> SjekkLedigAsync(BrukerRolle rolle, int? unntaBrukerId = null)
    {
        var type = LisensTypeExtensions.ForRolle(rolle);
        var tildelt = (await TildeltAsync()).GetValueOrDefault(type);
        if (tildelt <= 0)
        {
            return null;
        }

        var brukt = await db.Brukere.CountAsync(b => b.Aktiv && b.Rolle == rolle && (unntaBrukerId == null || b.Id != unntaBrukerId));
        return brukt >= tildelt
            ? $"Dere har brukt alle {tildelt} {type.Visningsnavn().ToLowerInvariant()}. Ta kontakt med Full Kontroll for å få aktivert flere."
            : null;
    }

    private async Task<Dictionary<LisensType, int>> TildeltAsync()
    {
        if (tenantContext.Current?.Id is not { } tenantId)
        {
            return [];
        }

        return await platformDb.TenantLisenser.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .GroupBy(l => l.Type)
            .Select(g => new { g.Key, Antall = g.Sum(l => l.AntallTildelt) })
            .ToDictionaryAsync(g => g.Key, g => g.Antall);
    }
}
