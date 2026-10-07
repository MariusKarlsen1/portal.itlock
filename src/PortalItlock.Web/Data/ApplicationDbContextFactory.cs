using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PortalItlock.Web.Data;

// Samme begrunnelse som PlatformDbContextFactory: uten en design-time factory
// for BEGGE kontekstene i prosjektet, må "dotnet ef" fortsatt bygge hele
// appens vertsobjekt for å validere/liste tilgjengelige DbContext-typer - selv
// når man kjører migrations mot PlatformDbContext med en egen factory - fordi
// ApplicationDbContext fortsatt mangler én. Det kjører Program.cs sin
// oppstartslogikk mot de faktiske databasefilene på disk, som feiler dersom en
// av dem nettopp fikk endret skjema i koden uten at migreringen er kjørt ennå.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")!;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
