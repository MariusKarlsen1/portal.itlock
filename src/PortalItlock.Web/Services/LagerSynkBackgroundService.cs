using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Lagerstyring fase 4: fører forbruk av reserverte varer automatisk (dør
// ferdig montert, arbeidsordre ferdigmeldt) for alle organisasjoner som har
// lagermodulen - også når ingen har lagersiden åpen. Hver organisasjon har
// sin egen database, så konteksten lages per leietaker her.
public class LagerSynkBackgroundService(IServiceScopeFactory scopeFactory, ILogger<LagerSynkBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervall = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
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
            var medLager = await platformDb.TenantModuler
                .Where(m => m.Modul == TilleggsModuler.Lager && m.Aktiv).Select(m => m.TenantId).ToListAsync(stoppingToken);
            leietakere = await platformDb.Tenants.AsNoTracking()
                .Where(t => t.Status == TenantStatus.Aktiv && medLager.Contains(t.Id)).ToListAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lagersynk: klarte ikke å hente organisasjoner med lagermodul.");
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
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(t.ConnectionString)
                    .AddInterceptors(new SqliteUnicodeFunctionsInterceptor())
                    .Options;
                await using var db = new ApplicationDbContext(options);
                var antall = await new PlukkService(db, new LagerService(db)).SynkForbrukAsync();
                if (antall > 0)
                {
                    logger.LogInformation("Lagersynk: {Antall} stk ført som forbruk for {Organisasjon}.", antall, t.Navn);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lagersynk feilet for {Organisasjon}.", t.Navn);
            }
        }
    }
}
