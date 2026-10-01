using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Scoped per forespørsel (minimal API-endepunkter / prerendering) ELLER per
// Blazor Server-krets (samme DI-scope i hele brukerøkten etter at SignalR har
// koblet til). Verdien slås opp FØRSTE gang noen leser Current, og caches i
// resten av scopet.
public interface ITenantContext
{
    Tenant? Current { get; }
}

public class TenantContext(
    IHttpContextAccessor httpContextAccessor,
    NavigationManager navigationManager,
    PlatformDbContext platformDb) : ITenantContext
{
    private Tenant? _current;
    private bool _resolved;

    public Tenant? Current
    {
        get
        {
            if (!_resolved)
            {
                _current = Sla();
                _resolved = true;
            }
            return _current;
        }
    }

    // Vanlige HTTP-forespørsler (minimal API-endepunkter, og den første
    // prerenderingen av en Blazor-side) har en ekte HttpContext å lese
    // Host-headeren fra. Den interaktive Blazor-kretsen (etter at SignalR har
    // koblet til) har IKKE det lenger - DI-scopet for selve kretsen er
    // adskilt fra forespørselen som startet den - men NavigationManager er
    // da initialisert med nettleserens faktiske adresse, og brukes i stedet.
    private Tenant? Sla()
    {
        var host = httpContextAccessor.HttpContext?.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            try
            {
                host = new Uri(navigationManager.Uri).Host;
            }
            catch (InvalidOperationException)
            {
                host = null;
            }
        }

        var pavertsnavn = string.IsNullOrEmpty(host)
            ? null
            : platformDb.Tenants.FirstOrDefault(t => t.Subdomene == host && t.Status == TenantStatus.Aktiv);

        return pavertsnavn ?? platformDb.Tenants.FirstOrDefault(t => t.ErStandard && t.Status == TenantStatus.Aktiv);
    }
}
