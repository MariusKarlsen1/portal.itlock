using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Oppretter en splitter ny kunde (tenant): egen SQLite-fil, migrert og seedet
// akkurat som alle andre, pluss kundens egen første admin-bruker. Brukes kun
// fra plattform-admin-siden - ikke tilgjengelig for kundenes egne brukere.
public class TenantProvisioningService(PlatformDbContext platformDb, IConfiguration config)
{
    public async Task<(bool Ok, string Feilmelding)> OpprettKundeAsync(
        string navn, string vertsnavn, string adminNavn, string adminEpost)
    {
        navn = navn.Trim();
        vertsnavn = vertsnavn.Trim().ToLowerInvariant();
        adminNavn = adminNavn.Trim();
        adminEpost = adminEpost.Trim();

        if (string.IsNullOrWhiteSpace(navn) || string.IsNullOrWhiteSpace(vertsnavn)
            || string.IsNullOrWhiteSpace(adminNavn) || string.IsNullOrWhiteSpace(adminEpost))
        {
            return (false, "Alle felt må fylles ut.");
        }

        if (await platformDb.Tenants.AnyAsync(t => t.Subdomene == vertsnavn))
        {
            return (false, $"Vertsnavnet \"{vertsnavn}\" er allerede i bruk av en annen kunde.");
        }

        var defaultConnectionString = config.GetConnectionString("DefaultConnection")!;
        var dataMappe = PlatformConnectionStringHelper.FinnDataMappe(defaultConnectionString);
        var filsti = Path.Combine(dataMappe, $"{vertsnavn}.db");

        if (File.Exists(filsti))
        {
            return (false, $"Det finnes allerede en databasefil for \"{vertsnavn}\" på serveren.");
        }

        var connectionString = $"Data Source={filsti}";

        // Appen har etter hvert ~190 migrasjoner - å kjøre dem alle synkront
        // rett i den interaktive Blazor-kretsen fryser knappen i flere
        // minutter og risikerer at Railways proxy/SignalR-tilkoblingen tidsavbrytes
        // underveis. Kjøres derfor på en egen trådpool-tråd (Task.Run), og
        // "synchronous=NORMAL" på denne SPLITTER NYE, tomme filen gjør selve
        // migreringen mye raskere enn standard full fsync per migrasjon -
        // trygt her siden det ikke finnes noen eksisterende data å miste.
        await Task.Run(() =>
        {
            var tenantOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            using var nyDb = new ApplicationDbContext(tenantOptions);
            nyDb.Database.OpenConnection();
            nyDb.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");
            nyDb.Database.Migrate();
            TenantSeedHelper.SeedNyheter(nyDb);

            nyDb.Brukere.Add(new Bruker
            {
                Navn = adminNavn,
                Epost = adminEpost,
                Rolle = BrukerRolle.Admin
            });
            nyDb.SaveChanges();
        });

        platformDb.Tenants.Add(new Tenant
        {
            Navn = navn,
            Subdomene = vertsnavn,
            ConnectionString = connectionString,
            ErStandard = false
        });
        await platformDb.SaveChangesAsync();

        return (true, "");
    }
}
