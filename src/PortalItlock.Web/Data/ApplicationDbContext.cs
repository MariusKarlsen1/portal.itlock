using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<RequirementDimension> RequirementDimensions => Set<RequirementDimension>();
    public DbSet<RequirementValue> RequirementValues => Set<RequirementValue>();
    public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageRequirement> PackageRequirements => Set<PackageRequirement>();
    public DbSet<PackageComponent> PackageComponents => Set<PackageComponent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackageRequirement>(entity =>
        {
            entity.HasKey(pr => new { pr.PackageId, pr.RequirementValueId });

            entity.HasOne(pr => pr.Package)
                .WithMany(p => p.Krav)
                .HasForeignKey(pr => pr.PackageId);

            entity.HasOne(pr => pr.RequirementValue)
                .WithMany(rv => rv.Pakker)
                .HasForeignKey(pr => pr.RequirementValueId);
        });

        modelBuilder.Entity<PackageComponent>(entity =>
        {
            entity.HasKey(pc => new { pc.PackageId, pc.ComponentId });

            entity.HasOne(pc => pc.Package)
                .WithMany(p => p.Komponenter)
                .HasForeignKey(pc => pc.PackageId);

            entity.HasOne(pc => pc.Component)
                .WithMany(c => c.Pakker)
                .HasForeignKey(pc => pc.ComponentId);
        });

        modelBuilder.Entity<RequirementValue>()
            .HasOne(rv => rv.Dimensjon)
            .WithMany(d => d.Verdier)
            .HasForeignKey(rv => rv.RequirementDimensionId);

        modelBuilder.Entity<Component>()
            .HasOne(c => c.Type)
            .WithMany(t => t.Komponenter)
            .HasForeignKey(c => c.ComponentTypeId);

        SeedReferenceData(modelBuilder);
    }

    // Kravdimensjoner, kravverdier og komponenttyper avklart i teknisk forslag §3.
    // Selve komponentene (produkter) og dørpakkene fylles inn av brukerne i portalen.
    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequirementDimension>().HasData(
            new RequirementDimension { Id = 1, Navn = "Dørtype", Rekkefolge = 1 },
            new RequirementDimension { Id = 2, Navn = "Brannklasse", Rekkefolge = 2 },
            new RequirementDimension { Id = 3, Navn = "Risikoklasse", Rekkefolge = 3 },
            new RequirementDimension { Id = 4, Navn = "Rømningskrav", Rekkefolge = 4 },
            new RequirementDimension { Id = 5, Navn = "Sikkerhetsklasse", Rekkefolge = 5 },
            new RequirementDimension { Id = 6, Navn = "Automatisk døråpner", Rekkefolge = 6 }
        );

        modelBuilder.Entity<RequirementValue>().HasData(
            new RequirementValue { Id = 1, RequirementDimensionId = 1, Verdi = "Innerdør", Rekkefolge = 1 },
            new RequirementValue { Id = 2, RequirementDimensionId = 1, Verdi = "Ytterdør", Rekkefolge = 2 },
            new RequirementValue { Id = 3, RequirementDimensionId = 1, Verdi = "Branndør", Rekkefolge = 3 },

            new RequirementValue { Id = 4, RequirementDimensionId = 2, Verdi = "Ingen", Rekkefolge = 1 },
            new RequirementValue { Id = 5, RequirementDimensionId = 2, Verdi = "EI30", Rekkefolge = 2 },
            new RequirementValue { Id = 6, RequirementDimensionId = 2, Verdi = "EI60", Rekkefolge = 3 },

            new RequirementValue { Id = 7, RequirementDimensionId = 3, Verdi = "RKL1", Rekkefolge = 1 },
            new RequirementValue { Id = 8, RequirementDimensionId = 3, Verdi = "RKL2", Rekkefolge = 2 },
            new RequirementValue { Id = 9, RequirementDimensionId = 3, Verdi = "RKL3", Rekkefolge = 3 },
            new RequirementValue { Id = 10, RequirementDimensionId = 3, Verdi = "RKL4", Rekkefolge = 4 },
            new RequirementValue { Id = 11, RequirementDimensionId = 3, Verdi = "RKL5", Rekkefolge = 5 },
            new RequirementValue { Id = 12, RequirementDimensionId = 3, Verdi = "RKL6", Rekkefolge = 6 },

            new RequirementValue { Id = 13, RequirementDimensionId = 4, Verdi = "Rømning", Rekkefolge = 1 },
            new RequirementValue { Id = 14, RequirementDimensionId = 4, Verdi = "Ikke rømning", Rekkefolge = 2 },
            new RequirementValue { Id = 15, RequirementDimensionId = 4, Verdi = "Tilbakerømning", Rekkefolge = 3 },

            new RequirementValue { Id = 16, RequirementDimensionId = 5, Verdi = "RC1", Rekkefolge = 1 },
            new RequirementValue { Id = 17, RequirementDimensionId = 5, Verdi = "RC2", Rekkefolge = 2 },
            new RequirementValue { Id = 18, RequirementDimensionId = 5, Verdi = "RC3", Rekkefolge = 3 },
            new RequirementValue { Id = 19, RequirementDimensionId = 5, Verdi = "RC4", Rekkefolge = 4 },
            new RequirementValue { Id = 20, RequirementDimensionId = 5, Verdi = "RC5", Rekkefolge = 5 },

            new RequirementValue { Id = 21, RequirementDimensionId = 6, Verdi = "Ja", Rekkefolge = 1 },
            new RequirementValue { Id = 22, RequirementDimensionId = 6, Verdi = "Nei", Rekkefolge = 2 }
        );

        modelBuilder.Entity<ComponentType>().HasData(
            new ComponentType { Id = 1, Navn = "Dørblad" },
            new ComponentType { Id = 2, Navn = "Karm" },
            new ComponentType { Id = 3, Navn = "Hengsler" },
            new ComponentType { Id = 4, Navn = "Terskel" },
            new ComponentType { Id = 5, Navn = "Låskasse" },
            new ComponentType { Id = 6, Navn = "Sluttstykke" },
            new ComponentType { Id = 7, Navn = "Dørvrider" },
            new ComponentType { Id = 8, Navn = "Sylinder/lås" },
            new ComponentType { Id = 9, Navn = "Panikkbeslag" },
            new ComponentType { Id = 10, Navn = "Dørpumpe" },
            new ComponentType { Id = 11, Navn = "Dørautomatikk" },
            new ComponentType { Id = 12, Navn = "Koordinering" },
            new ComponentType { Id = 13, Navn = "Kortleser" },
            new ComponentType { Id = 14, Navn = "Albuebryter" },
            new ComponentType { Id = 15, Navn = "Utspaseringsknapp" },
            new ComponentType { Id = 16, Navn = "Koblingsboks" }
        );
    }
}
