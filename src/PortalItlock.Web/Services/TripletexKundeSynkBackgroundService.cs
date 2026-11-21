using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Holder kundene like i portalen og Tripletex for alle organisasjoner som har
// Tripletex koblet: nye og endrede kunder (med kontaktpersoner) sendes dit, og
// kunder opprettet eller endret i Tripletex hentes inn. Hvert 10. minutt.
public class TripletexKundeSynkBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TripletexKundeSynkBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervall = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Intervall);
        while (!stoppingToken.IsCancellationRequested)
        {
            await KjorAsync(stoppingToken);
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

    private async Task KjorAsync(CancellationToken stoppingToken)
    {
        List<Tenant> leietakere;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            leietakere = await platformDb.Tenants.AsNoTracking()
                .Where(t => t.Status == TenantStatus.Aktiv).ToListAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tripletex-kundesynk: klarte ikke å hente organisasjonene.");
            return;
        }

        foreach (var t in leietakere)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tripletex = scope.ServiceProvider.GetRequiredService<TripletexNokkelOppslag>().Lag(t);
                if (!tripletex.ErKonfigurert)
                {
                    continue;
                }

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(t.ConnectionString)
                    .AddInterceptors(new SqliteUnicodeFunctionsInterceptor())
                    .Options;
                await using var db = new ApplicationDbContext(options);
                var r = await new TripletexSyncService(db, tripletex).SynkroniserKunderAsync(stoppingToken);
                if (r.PushetTilTripletex + r.HentetFraTripletex > 0 || r.Feil.Count > 0)
                {
                    logger.LogInformation("Tripletex-kundesynk for {Organisasjon}: {Sendt} sendt, {Hentet} hentet, {Oppdatert} oppdatert, {Feil} feil.",
                        t.Navn, r.PushetTilTripletex, r.HentetFraTripletex, r.Oppdatert, r.Feil.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tripletex-kundesynk feilet for {Organisasjon}.", t.Navn);
            }
        }
    }
}
