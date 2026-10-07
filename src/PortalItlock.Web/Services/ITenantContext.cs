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
        // Innlogget bruker: organisasjonen ligger som en claim på
        // innloggingscookien (satt i /account/login, som kan ha autentisert
        // mot en HELT ANNEN organisasjon enn den gjeldende verten tilhører -
        // f.eks. når flere organisasjoner deler samme adresse). Vinner alltid
        // over vertsnavn-oppslag når den finnes og fortsatt er gyldig, slik
        // at man ikke plutselig havner i feil organisasjons data bare fordi
        // man er innlogget på delt domene. Krever at UseAuthentication() har
        // kjørt FØR denne tjenesten første gang leses i pipelinen (se
        // Program.cs - de to egendefinerte middlewarene ligger derfor etter
        // UseAuthentication/UseAuthorization, ikke før).
        var tenantIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        if (tenantIdClaim is not null && int.TryParse(tenantIdClaim, out var tenantId))
        {
            var fraInnlogging = platformDb.Tenants.FirstOrDefault(t => t.Id == tenantId && t.Status == TenantStatus.Aktiv);
            if (fraInnlogging is not null)
            {
                return fraInnlogging;
            }
        }

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

        if (string.IsNullOrEmpty(host))
        {
            return platformDb.Tenants.FirstOrDefault(t => t.ErStandard && t.Status == TenantStatus.Aktiv);
        }

        var eksakt = platformDb.Tenants.FirstOrDefault(t => t.Subdomene == host && t.Status == TenantStatus.Aktiv);
        if (eksakt is not null)
        {
            return eksakt;
        }

        // Lokal utvikling (localhost og *.localhost) faller alltid tilbake til
        // standard-kunden, slik at lokal testing virker uten eget DNS-oppsett.
        // I produksjon gir derimot et vertsnavn som ikke matcher noen kunde
        // IKKE lenger automatisk itlock sine data (slik det gjorde før) -
        // TenantRedirectMiddleware sender i stedet brukeren til "Finn min
        // side", f.eks. for det bare produkt-domenet uten subdomene.
        if (host == "localhost" || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return platformDb.Tenants.FirstOrDefault(t => t.ErStandard && t.Status == TenantStatus.Aktiv);
        }

        return null;
    }
}
