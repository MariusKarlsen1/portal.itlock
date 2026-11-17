using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Moduler som admin kan gi enkeltbrukere i tillegg til det rollen gir
// (Bruker.TildelteModuler, kommaliste med modulens Href). Eksempel: en montør
// som skal hjelpe til med oppsett får «Komponenter og priser».
public static class ModulTildeling
{
    public const string KomponentPris = "komponentprismodul";
    public const string KomponentPrisPolicy = "Modul:komponentprismodul";

    public sealed record Tildelbar(string Href, string Tittel, string Beskrivelse);

    public static readonly Tildelbar[] Tildelbare =
    [
        new(KomponentPris, "Komponenter og priser", "Komponentregister, prisimport, leverandører, rabattgrupper og produktgrupper"),
    ];

    public static HashSet<string> Les(string? tildelte) =>
        (tildelte ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(h => Tildelbare.Any(t => t.Href == h))
            .ToHashSet();

    public static string? Skriv(IEnumerable<string> moduler)
    {
        var liste = moduler.Where(h => Tildelbare.Any(t => t.Href == h)).Distinct().Order().ToList();
        return liste.Count == 0 ? null : string.Join(",", liste);
    }
}

public sealed record ModulKrav(string Modul) : IAuthorizationRequirement;

// Admin har alltid tilgang. Andre må ha fått modulen på brukeren sin. Leses
// fra databasen ved hver sjekk, så tildeling og fjerning virker med en gang.
public sealed class ModulKravHandler(ApplicationDbContext db) : AuthorizationHandler<ModulKrav>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ModulKrav krav)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(krav);
            return;
        }
        if (!int.TryParse(context.User.FindFirst("BrukerId")?.Value, out var brukerId))
        {
            return;
        }
        var tildelte = await db.Brukere.AsNoTracking()
            .Where(b => b.Id == brukerId && b.Aktiv && b.SlettetDato == null)
            .Select(b => b.TildelteModuler)
            .FirstOrDefaultAsync();
        if (ModulTildeling.Les(tildelte).Contains(krav.Modul))
        {
            context.Succeed(krav);
        }
    }
}
