namespace PortalItlock.Web.Services;

public class TicketEskaleringBackgroundService(OrganisasjonKjorer kjorer, ILogger<TicketEskaleringBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await kjorer.ForHverAsync("Ticket-eskalering", async (sp, _) =>
                {
                    var eskaleringService = sp.GetRequiredService<TicketEskaleringService>();
                    await eskaleringService.SjekkOgVarsleAsync();
                }, stoppingToken);
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
