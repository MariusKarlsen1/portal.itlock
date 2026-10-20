using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Engangs-omlegging fra én felles brukerlisens til én lisens per rolle
// (admin, prosjektleder, montør, driftslisens). Kjøres ved oppstart for hver
// organisasjon, og gjør ingenting når den allerede er lagt om.
public static class LisensOmlegging
{
    public static void Kjor(PlatformDbContext platformDb, ApplicationDbContext tenantDb, Tenant tenant)
    {
        var lisenser = platformDb.TenantLisenser.Where(l => l.TenantId == tenant.Id).ToList();
        int Tildelt(LisensType t) => lisenser.Where(l => l.Type == t).Sum(l => l.AntallTildelt);
        var aktive = tenantDb.Brukere.Where(b => b.Aktiv).Select(b => b.Rolle).ToList();
        int IBruk(BrukerRolle r) => aktive.Count(x => x == r);

        var gammel = lisenser.FirstOrDefault(l => l.Type == LisensType.Brukere);
        var harNye = lisenser.Any(l => LisensTypeExtensions.Ansatte.Contains(l.Type));
        var endret = false;

        if (!harNye)
        {
            // Den gamle lisensen fordeles etter rollene brukerne har i dag;
            // resten av de tildelte går til montør.
            var totalt = gammel?.AntallTildelt ?? 0;
            var admin = IBruk(BrukerRolle.Admin);
            var pl = IBruk(BrukerRolle.Prosjektleder);
            var montor = Math.Max(IBruk(BrukerRolle.Montor), totalt - admin - pl);
            if (totalt == 0)
            {
                admin = pl = montor = 0;
            }

            platformDb.TenantLisenser.AddRange(
                new TenantLisens { TenantId = tenant.Id, Type = LisensType.Admin, AntallTildelt = admin },
                new TenantLisens { TenantId = tenant.Id, Type = LisensType.Prosjektleder, AntallTildelt = pl },
                new TenantLisens { TenantId = tenant.Id, Type = LisensType.Montor, AntallTildelt = montor });
            if (gammel is not null)
            {
                gammel.AntallTildelt = 0;
            }

            platformDb.TenantHendelser.Add(new TenantHendelse
            {
                TenantId = tenant.Id,
                Tekst = $"Brukerlisenser delt opp: admin {admin}, prosjektleder {pl}, montør {montor}",
                Ikon = "credit-card",
                Av = "Automatisk"
            });
            endret = true;
        }

        if (lisenser.All(l => l.Type != LisensType.Driftslisens))
        {
            // Driftslisenser hadde ingen grense før: de som finnes, blir tildelt.
            var drift = IBruk(BrukerRolle.Driftslisens);
            platformDb.TenantLisenser.Add(new TenantLisens { TenantId = tenant.Id, Type = LisensType.Driftslisens, AntallTildelt = drift });
            endret = true;
        }

        if (lisenser.All(l => l.Type != LisensType.KundeTilgang))
        {
            platformDb.TenantLisenser.Add(new TenantLisens { TenantId = tenant.Id, Type = LisensType.KundeTilgang, AntallTildelt = Tildelt(LisensType.KundeTilgang) });
            endret = true;
        }

        if (endret)
        {
            platformDb.SaveChanges();
        }
    }
}
