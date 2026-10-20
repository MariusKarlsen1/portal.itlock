namespace PortalItlock.Web.Services;

public class ServiceVarselBackgroundService(OrganisasjonKjorer kjorer, ILogger<ServiceVarselBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await kjorer.ForHverAsync("Serviceavtale-varsler", async (sp, _) =>
                {
                    var varselService = sp.GetRequiredService<ServiceVarselService>();
                    await varselService.SjekkOgSendVarselAsync();
                    await varselService.OpprettOppfolgingsTicketerAsync();
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under sjekk av serviceavtale-varsler.");
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
