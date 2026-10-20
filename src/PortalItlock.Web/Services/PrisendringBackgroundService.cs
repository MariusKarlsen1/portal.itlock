namespace PortalItlock.Web.Services;

public class PrisendringBackgroundService(OrganisasjonKjorer kjorer, ILogger<PrisendringBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await kjorer.ForHverAsync("Planlagte prisendringer", async (sp, _) =>
                {
                    var utforerService = sp.GetRequiredService<PrisendringUtforerService>();
                    await utforerService.UtforForfalteAsync();
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under utforelse av planlagte prisendringer.");
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
