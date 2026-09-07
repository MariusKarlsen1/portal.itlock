namespace PortalItlock.Web.Services;

public class TicketEskaleringBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TicketEskaleringBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var eskaleringService = scope.ServiceProvider.GetRequiredService<TicketEskaleringService>();
                await eskaleringService.SjekkOgVarsleAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under sjekk av ticket-eskalering.");
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
