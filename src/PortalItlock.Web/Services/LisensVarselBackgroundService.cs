namespace PortalItlock.Web.Services;

public class LisensVarselBackgroundService(OrganisasjonKjorer kjorer, ILogger<LisensVarselBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await kjorer.ForHverAsync("Lisensvarsler", async (sp, _) =>
                {
                    var varselService = sp.GetRequiredService<LisensVarselService>();
                    await varselService.SjekkOgSendVarselAsync();
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under sjekk av lisensvarsler.");
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
