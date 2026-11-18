using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Rollen til den som er logget inn, for sperrer i metodene (ikke bare skjulte
// knapper). Reglene står i ModulRettigheter.
public static class Tilgang
{
    public static async Task<BrukerRolle?> RolleAsync(AuthenticationStateProvider auth)
    {
        var state = await auth.GetAuthenticationStateAsync();
        var verdi = state.User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<BrukerRolle>(verdi, out var rolle) ? rolle : null;
    }

    // Slette noe i portalen: bare admin og prosjektleder (ikke montører).
    public static async Task<bool> KanSletteAsync(AuthenticationStateProvider auth) =>
        ModulRettigheter.KanSlette(await RolleAsync(auth));

    public static async Task<bool> KanRedigereAsync(AuthenticationStateProvider auth, string modul) =>
        ModulRettigheter.KanRedigere(await RolleAsync(auth), modul);
}
