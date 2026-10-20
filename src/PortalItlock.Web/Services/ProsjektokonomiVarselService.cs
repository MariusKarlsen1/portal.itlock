using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Ukentlig påminnelse (mandag morgen) om prosjektøkonomi: pågående prosjekter
// med gult eller rødt varsel, og overleverte prosjekter som venter på
// etterkalkyle. Sendes bare når det er noe å følge opp.
public class ProsjektokonomiVarselService(
    ProsjektOkonomiService okonomi,
    VarselMottakerService mottakere,
    EmailService epost,
    PlatformDbContext platformDb,
    ITenantContext tenantContext,
    ILogger<ProsjektokonomiVarselService> logger)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public async Task SjekkOgSendAsync()
    {
        if (tenantContext.Current is not { } tenant)
        {
            return;
        }

        // Mandag fra kl. 06, én gang per uke.
        var na = DateTime.Now;
        var mandag = na.Date.AddDays(-(((int)na.DayOfWeek + 6) % 7));
        if (na < mandag.AddHours(6))
        {
            return;
        }

        var inn = await platformDb.TenantInnstillinger.FirstOrDefaultAsync(i => i.TenantId == tenant.Id);
        if (inn?.SistOkonomiVarsel is { } sist && sist >= mandag)
        {
            return;
        }

        var aktive = await okonomi.AktiveVunneIderAsync();
        var tall = aktive.Count == 0 ? [] : await okonomi.BeregnFlereAsync(aktive);
        var varsel = tall.Values
            .Where(o => o.Status is ProsjektOkonomiService.OkonomiStatus.FolgMed or ProsjektOkonomiService.OkonomiStatus.OverBudsjett)
            .ToList();
        var tilGjennomgang = await okonomi.TilGjennomgangAsync();

        if (varsel.Count > 0 || tilGjennomgang.Count > 0)
        {
            var html = await LagHtmlAsync(varsel, tilGjennomgang);
            var emne = $"Prosjektøkonomi: {varsel.Count} å følge med, {tilGjennomgang.Count} til etterkalkyle";
            await mottakere.SendAsync(epost, VarselType.Prosjektokonomi, emne, html, logger);
        }

        if (inn is null)
        {
            inn = new TenantInnstillinger { TenantId = tenant.Id };
            platformDb.TenantInnstillinger.Add(inn);
        }

        inn.SistOkonomiVarsel = na;
        await platformDb.SaveChangesAsync();
    }

    private async Task<string> LagHtmlAsync(List<ProsjektOkonomiService.Okonomi> varsel, List<Prosjekt> tilGjennomgang)
    {
        static string Kr(decimal v) => v.ToString("#,##0", Kultur) + " kr";
        var navn = await okonomi.ProsjektnavnAsync(varsel.Select(o => o.ProsjektId));

        var html = "";
        if (varsel.Count > 0)
        {
            var rader = string.Join("", varsel
                .OrderBy(o => o.Status == ProsjektOkonomiService.OkonomiStatus.OverBudsjett ? 0 : 1)
                .Select(o => $"<tr><td>{WebUtility.HtmlEncode(navn.GetValueOrDefault(o.ProsjektId, $"P-{o.ProsjektId}"))}</td>"
                    + $"<td>{(o.Status == ProsjektOkonomiService.OkonomiStatus.OverBudsjett ? "Over budsjett" : "Følg med")}</td>"
                    + $"<td>{WebUtility.HtmlEncode(o.StatusTekst)}</td><td>{Kr(o.BudsjettKost)}</td><td>{Kr(o.PrognoseKost)}</td></tr>"));
            html += "<p><b>Pågående prosjekter med gult eller rødt varsel:</b></p>"
                + "<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\"><tr><th>Prosjekt</th><th>Status</th><th>Hvorfor</th><th>Budsjett</th><th>Prognose</th></tr>"
                + rader + "</table>";
        }

        if (tilGjennomgang.Count > 0)
        {
            var rader = string.Join("", tilGjennomgang
                .Select(p => $"<tr><td>{WebUtility.HtmlEncode(p.Navn)}</td><td>{p.OverlevertDato:dd.MM.yyyy}</td></tr>"));
            html += $"<p><b>Overleverte prosjekter som venter på etterkalkyle ({tilGjennomgang.Count}):</b></p>"
                + "<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\"><tr><th>Prosjekt</th><th>Overlevert</th></tr>"
                + rader + "</table>";
        }

        return html + "<p>Se Prosjektøkonomi i portalen for detaljer.</p>";
    }
}

public class ProsjektokonomiVarselBackgroundService(OrganisasjonKjorer kjorer, ILogger<ProsjektokonomiVarselBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await kjorer.ForHverAsync("Prosjektøkonomi-varsel", async (sp, _) =>
                    await sp.GetRequiredService<ProsjektokonomiVarselService>().SjekkOgSendAsync(), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under sjekk av prosjektøkonomi-varsel.");
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
