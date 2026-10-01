using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Scoped per Blazor Server-krets (DI-scopet lever like lenge som SignalR-
// kretsen, så én gang satt av TenantResolverMiddleware på den første HTTP-
// forespørselen i kretsen, gjenbrukes samme Tenant for hele brukerøkten).
public interface ITenantContext
{
    Tenant? Current { get; set; }
}

public class TenantContext : ITenantContext
{
    public Tenant? Current { get; set; }
}
