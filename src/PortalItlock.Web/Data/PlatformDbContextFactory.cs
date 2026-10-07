using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PortalItlock.Web.Data;

// Brukes kun av "dotnet ef" ved design-time (migrations) - uten denne må EF
// bygge hele appens vertsobjekt for å finne PlatformDbContext, noe som kjører
// Program.cs sin oppstartslogikk (bl.a. spørringer mot Tenants-tabellen) mot
// den FAKTISKE databasefilen på disk. Endrer man Tenant-modellen uten at
// migreringen er kjørt ennå, feiler den spørringen midt i oppstarten og
// stopper "dotnet ef migrations add" fra å fullføre. Denne factory-en lager
// konteksten direkte fra appsettings.json i stedet, uten å røre resten av
// oppstartskoden.
public class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var defaultConnectionString = config.GetConnectionString("DefaultConnection")!;
        var platformConnectionString = PlatformConnectionStringHelper.AvledFra(defaultConnectionString);

        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseSqlite(platformConnectionString);

        return new PlatformDbContext(optionsBuilder.Options);
    }
}
