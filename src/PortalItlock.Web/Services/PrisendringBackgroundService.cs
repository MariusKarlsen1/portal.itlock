namespace PortalItlock.Web.Services;

public class PrisendringBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PrisendringBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var utforerService = scope.ServiceProvider.GetRequiredService<PrisendringUtforerService>();
                await utforerService.UtforForfalteAsync();
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
