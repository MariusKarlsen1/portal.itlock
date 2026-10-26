using System.Net;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Sender e-postvarsel til ansvarlig når en kundeoppfølging (kundekort → Oppfølging)
// når varseldatoen (på dagen / dagen før / uka før). Varsel i portalen (bjella og
// Hjem) går via Kunde.NesteOppfolgingsDato og trenger ikke denne tjenesten.
public class KundeOppfolgingVarselService(ApplicationDbContext db, EmailService epost, ILogger<KundeOppfolgingVarselService> logger)
{
    public async Task<int> SjekkOgSendVarselAsync()
    {
        // Varsler sendes fra kl. 08:00 på varseldagen.
        if (DateTime.Now.Hour < 8)
        {
            return 0;
        }

        var idag = DateTime.Today;
        var kandidater = await db.KundeOppfolginger
            .Include(o => o.Kunde)
            .Include(o => o.AnsvarligBruker)
            .Where(o => !o.Utfort && o.VarselEpost && !o.EpostVarselSendt && o.AnsvarligBruker != null)
            .ToListAsync();

        var sendt = 0;
        foreach (var o in kandidater.Where(o => o.VarselDato <= idag))
        {
            var kunde = WebUtility.HtmlEncode(o.Kunde?.Navn ?? "kunde");
            var html = $"<p>Hei {WebUtility.HtmlEncode(o.AnsvarligBruker!.Navn)},</p>"
                + $"<p>Påminnelse: <b>{o.Type.Visningsnavn()}: {WebUtility.HtmlEncode(o.Tittel)}</b><br>"
                + $"Kunde: {kunde}<br>Når: {o.Frist:dd.MM.yyyy HH:mm}"
                + (string.IsNullOrWhiteSpace(o.Kontaktperson) ? "" : $"<br>Kontaktperson: {WebUtility.HtmlEncode(o.Kontaktperson)}")
                + (string.IsNullOrWhiteSpace(o.Notat) ? "" : $"<br>Notat: {WebUtility.HtmlEncode(o.Notat)}")
                + "</p><p>Åpne kundekortet i portalen for å logge kontakten eller utsette.</p>";

            if (await epost.SendAsync(o.AnsvarligBruker.Epost, $"Følg opp {o.Kunde?.Navn}: {o.Tittel}", html))
            {
                o.EpostVarselSendt = true;
                sendt++;
            }
            else
            {
                logger.LogWarning("Klarte ikke å sende oppfølgingsvarsel {Id} til {Mottaker}.", o.Id, o.AnsvarligBruker.Epost);
            }
        }

        if (sendt > 0)
        {
            await db.SaveChangesAsync();
        }

        return sendt;
    }
}

public class KundeOppfolgingVarselBackgroundService(OrganisasjonKjorer kjorer, ILogger<KundeOppfolgingVarselBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await kjorer.ForHverAsync("Oppfølgingsvarsler", async (sp, _) =>
                {
                    await sp.GetRequiredService<KundeOppfolgingVarselService>().SjekkOgSendVarselAsync();
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under sjekk av oppfølgingsvarsler.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
