namespace PortalItlock.Web.Services;

public class LisensVarselBackgroundService(IServiceScopeFactory scopeFactory, ILogger<LisensVarselBackgroundService> logger) : BackgroundService
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
                var varselService = scope.ServiceProvider.GetRequiredService<LisensVarselService>();
                await varselService.SjekkOgSendVarselAsync();
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
