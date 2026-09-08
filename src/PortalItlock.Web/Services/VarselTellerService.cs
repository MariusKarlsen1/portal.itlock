using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public record VarselTeller(
    int AvvikSomVenter,
    int ServiceavtalerVarsel,
    int KunderTrengerOppfolging,
    int VarerLavBeholdning,
    int FravarSoknaderVenter,
    int TilvalgVarsel,
    int DriftsmeldingerVarsel,
    int TicketerVarsel,
    int CeGodkjenningerVarsel,
    int KlarTilFaktureringVarsel)
{
    public int Totalt => AvvikSomVenter + ServiceavtalerVarsel + KunderTrengerOppfolging + VarerLavBeholdning
        + FravarSoknaderVenter + TilvalgVarsel + DriftsmeldingerVarsel + TicketerVarsel + CeGodkjenningerVarsel
        + KlarTilFaktureringVarsel;
}

// Delt mellom NavMenu (venstremeny-badge) og TopBar (brukermeny-badge), slik at
// begge viser samme varseltall uten å duplisere sporringene.
public class VarselTellerService(ApplicationDbContext db)
{
    public async Task<VarselTeller> HentAsync(bool visUtvidet, bool erAdmin)
    {
        var avvikSomVenter = await db.Avvik.CountAsync(a => a.Status == AvvikStatus.SendtTilKunde);

        var serviceavtalerVarsel = 0;
        var kunderTrengerOppfolging = 0;
        var varerLavBeholdning = 0;
        var ticketerVarsel = 0;
        var tilvalgVarsel = 0;
        var driftsmeldingerVarsel = 0;
        var ceGodkjenningerVarsel = 0;
        var klarTilFaktureringVarsel = 0;

        if (visUtvidet)
        {
            var serviceProsjektIder = await db.Prosjekter
                .Where(p => p.Status == ProsjektStatus.Serviceavtale)
                .Select(p => p.Id)
                .ToListAsync();

            if (serviceProsjektIder.Count > 0)
            {
                var runder = await db.Servicerunder
                    .Where(r => serviceProsjektIder.Contains(r.ProsjektId))
                    .ToListAsync();

                var terskel = DateTime.Today.AddDays(14);
                serviceavtalerVarsel = serviceProsjektIder.Count(pid =>
                {
                    var neste = runder.Where(r => r.ProsjektId == pid)
                        .OrderByDescending(r => r.Dato)
                        .FirstOrDefault(r => r.NesteServiceDato.HasValue)?.NesteServiceDato;
                    return neste.HasValue && neste.Value.Date <= terskel;
                });
            }

            kunderTrengerOppfolging = await db.Kunder.CountAsync(k =>
                k.NesteOppfolgingsDato != null && k.NesteOppfolgingsDato.Value.Date <= DateTime.Today);

            varerLavBeholdning = await db.Components.CountAsync(c =>
                c.Aktiv && c.ILagerstyring && c.Minimumsbeholdning != null && c.Lagerantall <= c.Minimumsbeholdning.Value);

            ticketerVarsel = await db.Tickets.CountAsync(t =>
                t.Status != TicketStatus.Lukket && t.AnsvarligBrukerId == null);

            tilvalgVarsel = await db.Tilvalg.CountAsync(t => t.Status == TilvalgStatus.Besvart && !t.LestAvAnsatt);
            driftsmeldingerVarsel = await db.Driftsmeldinger.CountAsync(m => !m.LestAvAnsatt);

            var ceTerskel = DateTime.Today.AddDays(30);
            ceGodkjenningerVarsel = await db.CeGodkjenninger.CountAsync(c =>
                c.Status == CeGodkjenningStatus.Godkjent && c.GyldigTil.Date <= ceTerskel);

            klarTilFaktureringVarsel = await db.Arbeidsordre.CountAsync(a => a.Status == ArbeidsordreStatus.Ferdig);
        }

        var fravarSoknaderVenter = erAdmin
            ? await db.FravarSoknader.CountAsync(f => f.Status == FravarStatus.Venter)
            : 0;

        return new VarselTeller(
            avvikSomVenter, serviceavtalerVarsel, kunderTrengerOppfolging, varerLavBeholdning,
            fravarSoknaderVenter, tilvalgVarsel, driftsmeldingerVarsel, ticketerVarsel, ceGodkjenningerVarsel,
            klarTilFaktureringVarsel);
    }
}
