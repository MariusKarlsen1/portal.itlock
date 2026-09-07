using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public class TicketEskaleringService(ApplicationDbContext db, EmailService epost, ILogger<TicketEskaleringService> logger)
{
    private const string MottakerEpost = "marius@itlock.no";

    public async Task<int> SjekkOgVarsleAsync()
    {
        var forfalte = await db.Tickets
            .Include(t => t.Kunde)
            .Where(t => t.Status != TicketStatus.Lukket
                && t.AnsvarligBrukerId == null
                && t.SlaFrist != null && t.SlaFrist < DateTime.Now
                && !t.EskaleringsVarselSendt)
            .ToListAsync();

        if (forfalte.Count == 0)
        {
            return 0;
        }

        var rader = string.Join("", forfalte
            .OrderBy(t => t.SlaFrist)
            .Select(t => $"<tr><td>#{t.Id}</td><td>{System.Net.WebUtility.HtmlEncode(t.Tittel)}</td><td>{System.Net.WebUtility.HtmlEncode(t.Kunde?.Navn ?? "-")}</td><td>{t.SlaFrist:dd.MM.yyyy HH:mm}</td></tr>"));

        var html = "<p>Følgende tickets har passert SLA-fristen og er ikke tildelt noen:</p>"
            + $"<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\"><tr><th>Nr</th><th>Tittel</th><th>Kunde</th><th>SLA-frist</th></tr>{rader}</table>"
            + "<p>Se Tickets i portalen for å tildele saken.</p>";

        var sendt = await epost.SendAsync(MottakerEpost, $"Tickets uten eier har passert SLA-fristen ({forfalte.Count})", html);
        if (!sendt)
        {
            logger.LogWarning("Klarte ikke å sende ticket-eskaleringsvarsel til {Mottaker}.", MottakerEpost);
            return 0;
        }

        foreach (var t in forfalte)
        {
            t.EskaleringsVarselSendt = true;
            db.TicketHendelser.Add(new TicketHendelse
            {
                TicketId = t.Id,
                Beskrivelse = "Automatisk eskaleringsvarsel sendt (SLA forfalt, ingen tildelt).",
                ErKundeSynlig = false
            });
        }

        await db.SaveChangesAsync();
        return forfalte.Count;
    }
}
