using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Tilbudsfrist på prosjekter: varsel fra dagen før fristen (og når den er
// passert) så lenge prosjektet står som «Tilbud under arbeid», altså at
// tilbudet ikke er laget og status ikke er endret til «Tilbud sendt».
public static class TilbudsfristVarsel
{
    // Frister til og med i morgen gir varsel.
    public static DateTime Terskel => DateTime.Today.AddDays(1);

    public static bool VenterPaTilbud(ProsjektStatus? status) =>
        status is null or ProsjektStatus.Aktiv;

    public static IQueryable<Prosjekt> Sporring(ApplicationDbContext db)
    {
        var terskel = Terskel.AddDays(1); // før midnatt etter i morgen
        return db.Prosjekter.Where(p => p.Tilbudsfrist != null && p.Tilbudsfrist < terskel
            && (p.Status == null || p.Status == ProsjektStatus.Aktiv));
    }
}
