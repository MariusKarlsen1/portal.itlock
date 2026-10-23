using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Finner Tripletex-nøklene for en organisasjon og lager en TripletexService
// for den. Alle organisasjoner, også itlock, bruker Full Kontroll sitt
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

        using var scope = scopeFactory.CreateScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        // Organisasjonen velger miljø (test eller produksjon), og har en
        // ansattnøkkel for hvert. Consumer-tokenet for miljøet er plattformens.
        var inn = platformDb.TenantInnstillinger.AsNoTracking()
            .Where(i => i.TenantId == tenant.Id)
            .Select(i => new { i.TripletexBrukTest, i.TripletexEmployeeTokenKryptert, i.TripletexTestEmployeeTokenKryptert })
            .FirstOrDefault();
        var test = inn?.TripletexBrukTest ?? oppsett.Hent().TripletexTest;
        var employee = hemmelig.Dekrypter(test ? inn?.TripletexTestEmployeeTokenKryptert : inn?.TripletexEmployeeTokenKryptert);
        var consumer = oppsett.ConsumerToken(test);
        if (!string.IsNullOrWhiteSpace(employee) && !string.IsNullOrWhiteSpace(consumer))
        {
            return new TripletexService.Nokler(consumer, employee, test);
        }

        // itlock brukte nøklene under Innstillinger før den fikk egen nøkkel.
        // Det fortsetter til egen ansattnøkkel er lagt inn under Oppsett → Tripletex.
        return tenant.ErStandard && oppsett.PlattformTripletex(test) is { } n
            ? new TripletexService.Nokler(n.ConsumerToken, n.EmployeeToken, n.Test)
            : null;
    }

    public TripletexService Lag(Tenant? tenant) => Lag(For(tenant), tenant?.Id ?? 0);

    public TripletexService Lag(TripletexService.Nokler? nokler, int tenantId)
    {
        var http = httpFactory.CreateClient(nameof(TripletexService));
        http.BaseAddress = new Uri(PlattformOppsettService.TripletexBaseUrl(nokler?.Test ?? true));
        return new TripletexService(http, nokler, tenantId);
    }
}
