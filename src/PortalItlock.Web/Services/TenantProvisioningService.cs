using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Oppretter en splitter ny kunde (tenant): egen SQLite-fil, migrert og seedet
// akkurat som alle andre, pluss kundens egen første admin-bruker. Brukes kun
// fra plattform-admin-siden - ikke tilgjengelig for kundenes egne brukere.
public class TenantProvisioningService(PlatformDbContext platformDb, IConfiguration config, TenantOppslagService oppslag)
{
    public async Task<(bool Ok, string Feilmelding)> OpprettKundeAsync(
        string navn, string? vertsnavn, string adminNavn, string adminEpost)
    {
        navn = navn.Trim();
        vertsnavn = vertsnavn?.Trim().ToLowerInvariant();
        adminNavn = adminNavn.Trim();
        adminEpost = adminEpost.Trim();

        if (string.IsNullOrWhiteSpace(navn) || string.IsNullOrWhiteSpace(adminNavn) || string.IsNullOrWhiteSpace(adminEpost))
        {
            return (false, "Firmanavn, admin-navn og admin-e-post må fylles ut.");
        }

        // Vertsnavn er valgfritt - organisasjoner logger som standard inn på
        // samme delte adresse og finnes igjen ut fra e-posten sin (se
        // TenantOppslagService), og trenger derfor ikke et eget domene for å
        // kunne bruke løsningen. Sett det kun hvis de faktisk skal ha sitt
        // eget domene i tillegg.
        if (!string.IsNullOrWhiteSpace(vertsnavn) && await platformDb.Tenants.AnyAsync(t => t.Subdomene == vertsnavn))
        {
            return (false, $"Vertsnavnet \"{vertsnavn}\" er allerede i bruk av en annen kunde.");
        }

        // E-post må være unik på tvers av ALLE organisasjoner nå som
        // innlogging kan skje på delt adresse - ellers vet ikke
        // TenantOppslagService hvilken organisasjon admin-brukeren faktisk tilhører.
        if (await oppslag.FinnTenantForEpostAsync(adminEpost) is not null)
        {
            return (false, $"E-posten \"{adminEpost}\" er allerede registrert hos en annen organisasjon.");
        }

        var defaultConnectionString = config.GetConnectionString("DefaultConnection")!;
        var dataMappe = PlatformConnectionStringHelper.FinnDataMappe(defaultConnectionString);

        string filBasis;
        if (string.IsNullOrWhiteSpace(vertsnavn))
        {
            filBasis = SlugifiserNavn(navn);
            var forsok = 0;
            while (File.Exists(Path.Combine(dataMappe, $"{filBasis}{(forsok == 0 ? "" : "-" + forsok)}.db")))
            {
                forsok++;
            }
            if (forsok > 0)
            {
                filBasis = $"{filBasis}-{forsok}";
            }
        }
        else
        {
            filBasis = vertsnavn;
            if (File.Exists(Path.Combine(dataMappe, $"{filBasis}.db")))
            {
                return (false, $"Det finnes allerede en databasefil for \"{vertsnavn}\" på serveren.");
            }
        }

        var filsti = Path.Combine(dataMappe, $"{filBasis}.db");
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

        var tenant = new Tenant
        {
            Navn = navn,
            Subdomene = string.IsNullOrWhiteSpace(vertsnavn) ? null : vertsnavn,
            ConnectionString = connectionString,
            ErStandard = false
        };
        platformDb.Tenants.Add(tenant);
        await platformDb.SaveChangesAsync();

        // Starter-kvote slik at de ikke er helt blokkert fra dag én - juster
        // opp/ned fra plattformsiden etter avtalt lisensomfang.
        platformDb.TenantLisenser.AddRange(
            new TenantLisens { TenantId = tenant.Id, Type = LisensType.Brukere, AntallTildelt = 3 },
            new TenantLisens { TenantId = tenant.Id, Type = LisensType.KundeTilgang, AntallTildelt = 10 });
        await platformDb.SaveChangesAsync();

        return (true, "");
    }

    private static string SlugifiserNavn(string navn)
    {
        var normalisert = navn.ToLowerInvariant()
            .Replace('æ', 'a').Replace('ø', 'o').Replace('å', 'a');
        var tegn = normalisert.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(tegn);
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }
        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "organisasjon" : slug;
    }
}
