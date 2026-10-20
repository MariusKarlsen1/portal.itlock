using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Sørger for at en bruker som deaktiveres eller slettes, mister tilgangen
// med en gang:
// - Innloggingscookien sjekkes mot databasen ved hver forespørsel (med en
//   kort cache), se OnValidatePrincipal i Program.cs.
// - Åpne Blazor-økter får beskjed via Utlogget, og UtloggingVakt i
//   layoutene sender brukeren til innloggingen.
public class BrukerTilgang(IMemoryCache cache, IServiceScopeFactory scopeFactory)
{
    private static readonly TimeSpan CacheTid = TimeSpan.FromSeconds(30);

    public event Action<int, int>? Utlogget;

    private static string Nokkel(int tenantId, int brukerId) => $"bruker-aktiv:{tenantId}:{brukerId}";

    public async Task<bool> ErAktivAsync(int tenantId, int brukerId)
    {
        if (cache.TryGetValue(Nokkel(tenantId, brukerId), out bool aktiv))
        {
            return aktiv;
        }

        using var scope = scopeFactory.CreateScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var tenant = await platformDb.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            // Organisasjonen håndteres av TenantContext (pause, slettet).
            return true;
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(tenant.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        aktiv = await db.Brukere.AnyAsync(b => b.Id == brukerId && b.Aktiv && b.SlettetDato == null);
        cache.Set(Nokkel(tenantId, brukerId), aktiv, CacheTid);
        return aktiv;
    }

    public void LoggUt(int tenantId, int brukerId)
    {
        cache.Set(Nokkel(tenantId, brukerId), false, CacheTid);
        Utlogget?.Invoke(tenantId, brukerId);
    }

    public void Glem(int tenantId, int brukerId) => cache.Remove(Nokkel(tenantId, brukerId));
}

// Deaktivering, aktivering og sletting av brukere. Brukes både av
// Brukere-siden i portalen og av adminportalen (med organisasjonens egen
// database). Gir feilmelding, eller null når det gikk bra.
public static class BrukerAdministrasjon
{
    public static async Task<string?> DeaktiverAsync(ApplicationDbContext db, BrukerTilgang tilgang, int tenantId, int brukerId, int? egenId)
    {
        var bruker = await db.Brukere.FirstOrDefaultAsync(b => b.Id == brukerId);
        if (bruker is null)
        {
            return "Fant ikke brukeren.";
        }

        if (brukerId == egenId)
        {
            return "Du kan ikke deaktivere deg selv.";
        }

        if (bruker.Rolle == BrukerRolle.Admin && !await db.Brukere.AnyAsync(b => b.Id != brukerId && b.Aktiv && b.SlettetDato == null && b.Rolle == BrukerRolle.Admin))
        {
            return "Organisasjonen må ha minst én aktiv administrator.";
        }

        bruker.Aktiv = false;
        await db.SaveChangesAsync();
        tilgang.LoggUt(tenantId, brukerId);
        return null;
    }

    public static async Task<string?> AktiverAsync(ApplicationDbContext db, BrukerTilgang tilgang, int tenantId, int brukerId, Func<BrukerRolle, int, Task<string?>>? sjekkLisens)
    {
        var bruker = await db.Brukere.FirstOrDefaultAsync(b => b.Id == brukerId && b.SlettetDato == null);
        if (bruker is null)
        {
            return "Fant ikke brukeren.";
        }

        if (sjekkLisens is not null && await sjekkLisens(bruker.Rolle, bruker.Id) is { } lisensFeil)
        {
            return lisensFeil;
        }

        bruker.Aktiv = true;
        await db.SaveChangesAsync();
        tilgang.Glem(tenantId, brukerId);
        return null;
    }

    // Sletter brukeren helt. Har brukeren timer, arbeidsordre eller annet som
    // må bli liggende, fjernes personen i stedet: ingen innlogging, e-posten
    // frigjøres, og brukeren vises ikke lenger, men registreringene står
    // fortsatt på navnet (merket «slettet»).
    public static async Task<(string? Feil, bool Helt)> SlettAsync(ApplicationDbContext db, BrukerTilgang tilgang, int tenantId, int brukerId, int? egenId)
    {
        var bruker = await db.Brukere.FirstOrDefaultAsync(b => b.Id == brukerId);
        if (bruker is null)
        {
            return ("Fant ikke brukeren.", false);
        }

        if (brukerId == egenId)
        {
            return ("Du kan ikke slette deg selv.", false);
        }

        if (bruker.Rolle == BrukerRolle.Admin && bruker.Aktiv && !await db.Brukere.AnyAsync(b => b.Id != brukerId && b.Aktiv && b.SlettetDato == null && b.Rolle == BrukerRolle.Admin))
        {
            return ("Organisasjonen må ha minst én aktiv administrator.", false);
        }

        var navn = bruker.Navn;
        db.Brukere.Remove(bruker);
        try
        {
            await db.SaveChangesAsync();
            tilgang.LoggUt(tenantId, brukerId);
            return (null, true);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }

        var beholdt = await db.Brukere.FirstAsync(b => b.Id == brukerId);
        beholdt.Navn = navn.EndsWith(" (slettet)") ? navn : $"{navn} (slettet)";
        beholdt.Epost = $"slettet-{brukerId}-{DateTime.Now:yyyyMMddHHmmss}@slettet.invalid";
        beholdt.PasswordHash = null;
        beholdt.Stilling = null;
        beholdt.Aktiv = false;
        beholdt.SlettetDato = DateTime.Now;
        await db.SaveChangesAsync();
        tilgang.LoggUt(tenantId, brukerId);
        return (null, false);
    }
}
