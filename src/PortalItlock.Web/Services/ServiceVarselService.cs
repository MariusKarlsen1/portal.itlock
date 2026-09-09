using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public class ServiceVarselService(ApplicationDbContext db, EmailService epost, ILogger<ServiceVarselService> logger)
{
    private const string MottakerEpost = "marius@itlock.no";

    public async Task<List<(Prosjekt Prosjekt, DateTime NesteServiceDato)>> FinnNaerForfallAsync()
    {
        var serviceProsjekter = await db.Prosjekter
            .Where(p => p.Status == ProsjektStatus.Serviceavtale)
            .ToListAsync();

        var naerForfall = new List<(Prosjekt Prosjekt, DateTime NesteServiceDato)>();
        if (serviceProsjekter.Count > 0)
        {
            var prosjektIder = serviceProsjekter.Select(p => p.Id).ToList();
            var runder = await db.Servicerunder
                .Where(r => prosjektIder.Contains(r.ProsjektId))
                .ToListAsync();

            var terskel = DateTime.Today.AddDays(14);
            foreach (var p in serviceProsjekter)
            {
                var neste = runder.Where(r => r.ProsjektId == p.Id)
                    .OrderByDescending(r => r.Dato)
                    .FirstOrDefault(r => r.NesteServiceDato.HasValue)?.NesteServiceDato;

                if (neste.HasValue && neste.Value.Date <= terskel)
                {
                    naerForfall.Add((p, neste.Value));
                }
            }
        }

        return naerForfall;
    }

    public async Task<(bool Sendt, int Antall)> SjekkOgSendVarselAsync(bool tvingSending = false)
    {
        var logg = await db.ServiceVarselSendt.FirstOrDefaultAsync();
        if (!tvingSending && logg is not null && logg.SistSendtDato.Date >= DateTime.Today)
        {
            return (false, 0);
        }

        var naerForfall = await FinnNaerForfallAsync();

        var sendt = false;
        if (naerForfall.Count > 0)
        {
            var rader = string.Join("", naerForfall
                .OrderBy(x => x.NesteServiceDato)
                .Select(x => $"<tr><td>{System.Net.WebUtility.HtmlEncode(x.Prosjekt.Navn)}</td><td>{x.NesteServiceDato:dd.MM.yyyy}</td></tr>"));

            var html = "<p>Følgende serviceavtaler nærmer seg forfall (innen 14 dager) eller er forfalt:</p>"
                + $"<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\"><tr><th>Avtale</th><th>Neste service</th></tr>{rader}</table>"
                + "<p>Se Service-modulen i portalen for detaljer.</p>";

            sendt = await epost.SendAsync(MottakerEpost, $"Serviceavtaler som nærmer seg forfall ({naerForfall.Count})", html);
            if (!sendt)
            {
                logger.LogWarning("Klarte ikke å sende serviceavtale-varsel til {Mottaker}.", MottakerEpost);
            }
        }

        if (sendt || naerForfall.Count == 0)
        {
            if (logg is null)
            {
                logg = new ServiceVarselSendt { SistSendtDato = DateTime.Today };
                db.ServiceVarselSendt.Add(logg);
            }
            else
            {
                logg.SistSendtDato = DateTime.Today;
            }
            await db.SaveChangesAsync();
        }

        return (sendt, naerForfall.Count);
    }

    // Oppretter automatisk en ticket for planlagt service når NesteServiceDato nærmer seg
    // (samme 14-dagers-terskel som e-postvarselet), slik at oppfølgingen faktisk havner i
    // arbeidskøen og ikke bare som et varsel noen må huske å følge opp manuelt.
    public async Task<int> OpprettOppfolgingsTicketerAsync()
    {
        var terskel = DateTime.Today.AddDays(14);

        var modneRunder = await db.Servicerunder
            .Include(r => r.Prosjekt).ThenInclude(p => p!.Kunde)
            .Where(r => !r.OppfolgingsTicketOpprettet
                && r.NesteServiceDato != null && r.NesteServiceDato.Value.Date <= terskel
                && r.Prosjekt!.Status == ProsjektStatus.Serviceavtale)
            .ToListAsync();

        // Kun den seneste runden per prosjekt er den reelle "neste service" - de andre er
        // historikk og skal ikke generere egne tickets.
        var sisteRundePerProsjekt = modneRunder
            .GroupBy(r => r.ProsjektId)
            .Select(g => g.OrderByDescending(r => r.Dato).First())
            .ToList();

        foreach (var runde in sisteRundePerProsjekt)
        {
            var prosjekt = runde.Prosjekt!;
            var ticket = new Ticket
            {
                Tittel = $"Planlagt service - {prosjekt.Navn}",
                Beskrivelse = $"Automatisk opprettet fra serviceavtalens oppfølging. Neste service var satt til {runde.NesteServiceDato:dd.MM.yyyy}."
                    + (string.IsNullOrWhiteSpace(runde.Anbefalinger) ? "" : $"\n\nAnbefalinger fra forrige runde: {runde.Anbefalinger}"),
                ProsjektId = prosjekt.Id,
                KundeId = prosjekt.KundeId,
                Prioritet = TicketPrioritet.Normal
            };
            ticket.Hendelser.Add(new TicketHendelse
            {
                Beskrivelse = "Opprettet automatisk fra serviceavtale-oppfølging."
            });
            db.Tickets.Add(ticket);

            foreach (var r in modneRunder.Where(r => r.ProsjektId == runde.ProsjektId))
            {
                r.OppfolgingsTicketOpprettet = true;
            }
        }

        if (sisteRundePerProsjekt.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        return sisteRundePerProsjekt.Count;
    }
}
