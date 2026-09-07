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
}
