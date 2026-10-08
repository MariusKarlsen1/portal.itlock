using Microsoft.Extensions.Options;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Holder kunder synkronisert mellom portalen og Tripletex løpende, uten at
// noen må huske å trykke en "synk nå"-knapp - se TripletexSyncService for
// selve logikken. Kjører kun hvis Tripletex faktisk er konfigurert
// (ConsumerToken/EmployeeToken satt), ellers venter den bare.
public sealed class TripletexSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<TripletexOptions> options,
    ILogger<TripletexSyncBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervall = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kort forsinkelse ved oppstart - ingen grunn til å konkurrere med
        // resten av appens egen oppstart om databasen/nettverket.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(options.Value.ConsumerToken) && !string.IsNullOrWhiteSpace(options.Value.EmployeeToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var tripletex = scope.ServiceProvider.GetRequiredService<TripletexService>();
                    var sync = new TripletexSyncService(db, tripletex);
                    await sync.SynkroniserKunderAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // Nettverksfeil/Tripletex nede skal ikke krasje bakgrunns-
                    // jobben for godt - bare prøv igjen neste runde. Logges nå
                    // (var tidligere en stille, tom catch - se CODE_REVIEW.md
                    // 2026-10-08), samme mønster som de andre bakgrunns-
                    // tjenestene, slik at f.eks. en utløpt Tripletex-nøkkel
                    // faktisk er synlig i loggen i stedet for usporbar.
                    logger.LogError(ex, "Feil under Tripletex-kundesynkronisering.");
                }
            }

            try
            {
                await Task.Delay(Intervall, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
