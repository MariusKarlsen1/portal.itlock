using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Delt mellom oppstarts-seedingen i Program.cs (kjøres mot alle eksisterende
// kunder) og TenantProvisioningService (kjøres mot én splitter ny kunde) -
// slik at begge stedene alltid seeder endringsloggen på nøyaktig samme måte.
public static class TenantSeedHelper
{
    public static void SeedNyheter(ApplicationDbContext db)
    {
        var endringsloggPath = Path.Combine(AppContext.BaseDirectory, "nyheter.json");
        var kjenteKildeIder = db.Nyheter.Where(n => n.KildeId != null).Select(n => n.KildeId!).ToHashSet();
        foreach (var innslag in EndringsloggLeser.LesAlle(endringsloggPath))
        {
            if (kjenteKildeIder.Contains(innslag.Id))
            {
                continue;
            }

            db.Nyheter.Add(new Nyhet
            {
                Tittel = innslag.Tittel,
                Innhold = innslag.Innhold,
                OpprettetDato = innslag.Dato.DateTime,
                KildeId = innslag.Id,
            });
            kjenteKildeIder.Add(innslag.Id);
        }
        db.SaveChanges();
    }
}
