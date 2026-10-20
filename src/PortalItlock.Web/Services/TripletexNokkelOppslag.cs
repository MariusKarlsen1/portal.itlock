using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Finner Tripletex-nøklene for en organisasjon og lager en TripletexService
// for den. itlock (standard-organisasjonen) bruker nøklene under
// Innstillinger i adminportalen. Andre organisasjoner bruker Full Kontroll sitt
// consumer-token og sin egen ansattnøkkel fra Oppsett → Tripletex.
public class TripletexNokkelOppslag(
    PlattformOppsettService oppsett,
    HemmelighetService hemmelig,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpFactory)
{
    public TripletexService.Nokler? For(Tenant? tenant)
    {
        if (tenant is null)
        {
            return null;
        }

        if (tenant.ErStandard)
        {
            return oppsett.ItlockTripletex() is { } n ? new TripletexService.Nokler(n.ConsumerToken, n.EmployeeToken, n.Test) : null;
        }

        using var scope = scopeFactory.CreateScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var kryptert = platformDb.TenantInnstillinger.AsNoTracking()
            .Where(i => i.TenantId == tenant.Id).Select(i => i.TripletexEmployeeTokenKryptert).FirstOrDefault();
        var employee = hemmelig.Dekrypter(kryptert);
        var (consumer, test) = oppsett.ConsumerToken();
        return string.IsNullOrWhiteSpace(employee) || string.IsNullOrWhiteSpace(consumer)
            ? null
            : new TripletexService.Nokler(consumer, employee, test);
    }

    public TripletexService Lag(Tenant? tenant) => Lag(For(tenant), tenant?.Id ?? 0);

    public TripletexService Lag(TripletexService.Nokler? nokler, int tenantId)
    {
        var http = httpFactory.CreateClient(nameof(TripletexService));
        http.BaseAddress = new Uri(PlattformOppsettService.TripletexBaseUrl(nokler?.Test ?? true));
        return new TripletexService(http, nokler, tenantId);
    }
}
