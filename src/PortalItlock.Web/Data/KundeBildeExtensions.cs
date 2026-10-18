using PortalItlock.Web.Models;

namespace PortalItlock.Web.Data;

// Kunde.Bilde er skilt ut (table splitting) - se Kunde.Bilde og
// KomponentFilerExtensions for hvorfor den ikke skal hentes via join.
public static class KundeBildeExtensions
{
    public static async Task<Kunde> LastBildeAsync(this ApplicationDbContext db, Kunde kunde)
    {
        if (kunde.Bilde is null)
        {
            await db.Entry(kunde).Reference(k => k.Bilde).LoadAsync();
        }

        return kunde;
    }

    public static Task<HashSet<int>> KunderMedBildeAsync(this ApplicationDbContext db, IReadOnlyCollection<int>? ider = null) =>
        db.RaderMedBlobAsync("Kunder", "BildeData", ider);
}