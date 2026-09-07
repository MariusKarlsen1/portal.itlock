namespace PortalItlock.Web.Services;

public class DatabaseBackupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DatabaseBackupBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var backupService = scope.ServiceProvider.GetRequiredService<DatabaseBackupService>();
                await backupService.KjorBackupAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under planlagt database-backup.");
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
