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

    // E-postene til testbrukerne. .invalid er et reservert toppnivådomene som
    // aldri kan rutes, så ingen e-post kan ved et uhell gå ut til dem.
    public const string TestkundeEpost = "testkunde@test.invalid";
    public const string TestvaktmesterEpost = "testvaktmester@test.invalid";

    // Oppretter én testkunde og én testvaktmester, slik at "Vis som Kunde" og
    // "Vis som Driftslisens" i toppfeltet har noen å logge inn som. Uten en
    // bruker med rollen har de to knappene ingenting å bytte til.
    //
    // Kjøres bare når det ikke finnes NOEN bruker med rollen fra før - har man
    // ekte kunder eller vaktmestere i portalen, røres ingenting. Idempotent:
    // finnes testbrukeren allerede, hoppes den over.
    //
    // Testbrukerne har ingen PasswordHash og kan derfor ikke logges inn på
    // vanlig vis - de finnes bare for rollebytteren. De kan trygt slettes i
    // Brukere; da lages de på nytt ved neste oppstart hvis det fortsatt ikke
    // finnes noen ekte bruker med rollen.
    public static void SeedTestbrukere(ApplicationDbContext db)
    {
        var endret = false;

        if (!db.Brukere.Any(b => b.Rolle == BrukerRolle.Kunde))
        {
            // Kobles til den første kunden som finnes, slik at kundeportalen
            // har noe å vise med en gang. Byttes i Kundetilganger.
            var forsteKunde = db.Kunder.OrderBy(k => k.Navn).FirstOrDefault();

            db.Brukere.Add(new Bruker
            {
                Navn = "Testkunde",
                Epost = TestkundeEpost,
                Rolle = BrukerRolle.Kunde,
                Stilling = "Testbruker for kundevisning",
                Aktiv = true,
                KundeId = forsteKunde?.Id
            });
            endret = true;
        }

        if (!db.Brukere.Any(b => b.Rolle == BrukerRolle.Driftslisens))
        {
            var testvaktmester = new Bruker
            {
                Navn = "Testvaktmester",
                Epost = TestvaktmesterEpost,
                Rolle = BrukerRolle.Driftslisens,
                Stilling = "Testbruker for driftsvisning",
                Aktiv = true
            };

            // Driftsportalen viser prosjektene vaktmesteren er satt på, så den
            // får det første aktive prosjektet for å ha noe å vise. Flere
            // legges til i Driftslisenser.
            var forsteProsjekt = db.Prosjekter
                .Where(p => p.Status == ProsjektStatus.Aktiv || p.Status == ProsjektStatus.UnderArbeid)
                .OrderBy(p => p.Navn)
                .FirstOrDefault();

            if (forsteProsjekt is not null)
            {
                testvaktmester.Prosjekter.Add(forsteProsjekt);
            }

            db.Brukere.Add(testvaktmester);
            endret = true;
        }

        if (endret)
        {
            db.SaveChanges();
        }
    }
}
