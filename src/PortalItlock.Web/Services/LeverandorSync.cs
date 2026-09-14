using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// En vare kan ha flere leverandørkoblinger (ComponentLeverandor), men bare én
// av dem er "standard" om gangen - denne klassen holder den regelen og
// speiler standard-koblingens Varenummer/Pris/Leverandornavn over på selve
// Component (Produktkode/PrisNetto/Leverandor), slik at alle de eksisterende
// stedene i appen som fortsatt leser disse feltene direkte (søk, filtre,
// visning) automatisk viser riktig verdi uten å måtte kjenne til den nye
// leverandørmodellen.
public static class LeverandorSync
{
    public static async Task<Leverandor> FinnEllerOpprettAsync(ApplicationDbContext db, string navn, CancellationToken ct = default)
    {
        var trimmet = navn.Trim();
        var eksisterende = await db.Leverandorer
            .FirstOrDefaultAsync(l => l.Navn.ToLower() == trimmet.ToLower(), ct);
        if (eksisterende is not null)
        {
            return eksisterende;
        }

        var ny = new Leverandor { Navn = trimmet };
        db.Leverandorer.Add(ny);
        return ny;
    }

    // Setter angitt leverandørkobling som standard for varen (og nullstiller
    // de andre), og speiler verdiene over på Component. Kaller ikke
    // SaveChangesAsync selv - det er opp til kalleren.
    public static async Task SettStandardAsync(ApplicationDbContext db, int componentId, int leverandorId, CancellationToken ct = default)
    {
        var component = await db.Components
            .Include(c => c.Leverandorer).ThenInclude(cl => cl.Leverandor)
            .FirstOrDefaultAsync(c => c.Id == componentId, ct);
        if (component is null)
        {
            return;
        }

        foreach (var link in component.Leverandorer)
        {
            link.ErStandard = link.LeverandorId == leverandorId;
        }

        var standard = component.Leverandorer.FirstOrDefault(l => l.LeverandorId == leverandorId);
        if (standard is null)
        {
            return;
        }

        component.Produktkode = standard.Varenummer;
        component.PrisNetto = standard.Pris;
        component.Leverandor = standard.Leverandor?.Navn;
    }
}
