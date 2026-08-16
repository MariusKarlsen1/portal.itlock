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
    public DbSet<DoorEnvironmentDocument> DoorEnvironmentDocuments => Set<DoorEnvironmentDocument>();
    public DbSet<Befaring> Befaringer => Set<Befaring>();
    public DbSet<BefaringDorfelt> BefaringDorfelt => Set<BefaringDorfelt>();
    public DbSet<BefaringLassystem> BefaringLassystemer => Set<BefaringLassystem>();
    public DbSet<BefaringDorfeltBilde> BefaringDorfeltBilder => Set<BefaringDorfeltBilde>();
    public DbSet<GuideSide> GuideSider => Set<GuideSide>();
    public DbSet<Nokkelsystem> Nokkelsystemer => Set<Nokkelsystem>();
    public DbSet<Rekvirent> Rekvirenter => Set<Rekvirent>();
    public DbSet<LasUtskifting> LasUtskiftinger => Set<LasUtskifting>();

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
            .HasForeignKey(c => c.ComponentTypeId)
            .IsRequired(false);

        SeedReferenceData(modelBuilder);
    }

    // Kravdimensjoner, kravverdier og komponenttyper hentet fra itlocks egen
    // "Beslagspakker Funksjoner Dørpakker flytskjema.xlsx" (Tabell1) - dette er
    // den reelle kravmodellen og komponentlisten firmaet faktisk bruker.
    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequirementDimension>().HasData(
            new RequirementDimension { Id = 1, Navn = "Type dør", Rekkefolge = 1 },
            new RequirementDimension { Id = 2, Navn = "Hvilke bruk", Rekkefolge = 2 },
            new RequirementDimension { Id = 3, Navn = "FG", Rekkefolge = 3 },
            new RequirementDimension { Id = 4, Navn = "Risikoklasse", Rekkefolge = 4 },
            new RequirementDimension { Id = 5, Navn = "Lukkefunksjon", Rekkefolge = 5 },
            new RequirementDimension { Id = 6, Navn = "Antall fløyer", Rekkefolge = 6 },
            new RequirementDimension { Id = 7, Navn = "Type beslag", Rekkefolge = 7 },
            new RequirementDimension { Id = 8, Navn = "Tilbakerømning", Rekkefolge = 8 },
            new RequirementDimension { Id = 9, Navn = "Postsonesylinder", Rekkefolge = 9 }
        );

        modelBuilder.Entity<RequirementValue>().HasData(
            new RequirementValue { Id = 1, RequirementDimensionId = 1, Verdi = "Innerdør", Rekkefolge = 1 },
            new RequirementValue { Id = 2, RequirementDimensionId = 1, Verdi = "Ytterdør", Rekkefolge = 2 },

            new RequirementValue { Id = 3, RequirementDimensionId = 2, Verdi = "Låsbar", Rekkefolge = 1 },
            new RequirementValue { Id = 4, RequirementDimensionId = 2, Verdi = "Ikke låsbar", Rekkefolge = 2 },
            new RequirementValue { Id = 5, RequirementDimensionId = 2, Verdi = "Toalett", Rekkefolge = 3 },
            new RequirementValue { Id = 6, RequirementDimensionId = 2, Verdi = "Forberedt for kortleser", Rekkefolge = 4 },
            new RequirementValue { Id = 7, RequirementDimensionId = 2, Verdi = "Med kortleser", Rekkefolge = 5 },
            new RequirementValue { Id = 8, RequirementDimensionId = 2, Verdi = "Med dørbladmontert kortleser", Rekkefolge = 6 },
            new RequirementValue { Id = 9, RequirementDimensionId = 2, Verdi = "Kun rømning RK 1-4", Rekkefolge = 7 },
            new RequirementValue { Id = 10, RequirementDimensionId = 2, Verdi = "Kun rømning RK 1-6", Rekkefolge = 8 },

            new RequirementValue { Id = 11, RequirementDimensionId = 3, Verdi = "Ikke FG", Rekkefolge = 1 },
            new RequirementValue { Id = 12, RequirementDimensionId = 3, Verdi = "FG", Rekkefolge = 2 },

            new RequirementValue { Id = 13, RequirementDimensionId = 4, Verdi = "Ikke rømning", Rekkefolge = 1 },
            new RequirementValue { Id = 14, RequirementDimensionId = 4, Verdi = "1-4", Rekkefolge = 2 },
            new RequirementValue { Id = 15, RequirementDimensionId = 4, Verdi = "1-6", Rekkefolge = 3 },

            new RequirementValue { Id = 16, RequirementDimensionId = 5, Verdi = "Ingen lukker", Rekkefolge = 1 },
            new RequirementValue { Id = 17, RequirementDimensionId = 5, Verdi = "Dørlukker", Rekkefolge = 2 },
            new RequirementValue { Id = 18, RequirementDimensionId = 5, Verdi = "Automatikk", Rekkefolge = 3 },
            new RequirementValue { Id = 19, RequirementDimensionId = 5, Verdi = "Dørlukker/automatikk", Rekkefolge = 4 },

            new RequirementValue { Id = 20, RequirementDimensionId = 6, Verdi = "1-fløyet", Rekkefolge = 1 },
            new RequirementValue { Id = 21, RequirementDimensionId = 6, Verdi = "2-fløyet", Rekkefolge = 2 },
            new RequirementValue { Id = 22, RequirementDimensionId = 6, Verdi = "Skyvedør", Rekkefolge = 3 },

            new RequirementValue { Id = 23, RequirementDimensionId = 7, Verdi = "Mekanisk", Rekkefolge = 1 },
            new RequirementValue { Id = 24, RequirementDimensionId = 7, Verdi = "Elektrisk", Rekkefolge = 2 },
            new RequirementValue { Id = 25, RequirementDimensionId = 7, Verdi = "Lukket/låst signal", Rekkefolge = 3 },
            new RequirementValue { Id = 26, RequirementDimensionId = 7, Verdi = "Hengelås", Rekkefolge = 4 },

            new RequirementValue { Id = 27, RequirementDimensionId = 8, Verdi = "Nei", Rekkefolge = 1 },
            new RequirementValue { Id = 28, RequirementDimensionId = 8, Verdi = "Ja", Rekkefolge = 2 },
            new RequirementValue { Id = 29, RequirementDimensionId = 8, Verdi = "Ikke aktuelt", Rekkefolge = 3 },

            new RequirementValue { Id = 30, RequirementDimensionId = 9, Verdi = "Nei", Rekkefolge = 1 },
            new RequirementValue { Id = 31, RequirementDimensionId = 9, Verdi = "Ja", Rekkefolge = 2 }
        );

        modelBuilder.Entity<ComponentType>().HasData(
            new ComponentType { Id = 1, Navn = "Låskasse 1" },
            new ComponentType { Id = 2, Navn = "Sluttstykke 1" },
            new ComponentType { Id = 3, Navn = "Stolpe 1" },
            new ComponentType { Id = 4, Navn = "Sylinder 1 utv" },
            new ComponentType { Id = 5, Navn = "Sylinder 1 innv." },
            new ComponentType { Id = 6, Navn = "Sylinder utstyr" },
            new ComponentType { Id = 7, Navn = "Sylinder utstyr 2" },
            new ComponentType { Id = 8, Navn = "Sylinder utstyr 3" },
            new ComponentType { Id = 9, Navn = "Håndtak" },
            new ComponentType { Id = 10, Navn = "Dørvrider" },
            new ComponentType { Id = 11, Navn = "Skilt 1" },
            new ComponentType { Id = 12, Navn = "Låskasse 2" },
            new ComponentType { Id = 13, Navn = "Sluttstykke 2" },
            new ComponentType { Id = 14, Navn = "Sluttstykke 2 utstyr" },
            new ComponentType { Id = 15, Navn = "Sylinder 2 utv" },
            new ComponentType { Id = 16, Navn = "Sylinder 2 innv" },
            new ComponentType { Id = 17, Navn = "Sylinder 2 utstyr" },
            new ComponentType { Id = 18, Navn = "Sylinder 2 utstyr 2" },
            new ComponentType { Id = 19, Navn = "Skilt 2" },
            new ComponentType { Id = 20, Navn = "Sylinderskruer" },
            new ComponentType { Id = 21, Navn = "Dørautomatikk" },
            new ComponentType { Id = 22, Navn = "Dørautomatikk arm/skinne" },
            new ComponentType { Id = 23, Navn = "Klemsikring bakkant" },
            new ComponentType { Id = 24, Navn = "Klemsikring forkant" },
            new ComponentType { Id = 25, Navn = "Kelmsikring karmoverføring" },
            new ComponentType { Id = 26, Navn = "Dørautomatikk utstyr" },
            new ComponentType { Id = 27, Navn = "Dørautomatikk utstyr 2" },
            new ComponentType { Id = 28, Navn = "Dørautomatikk blindplugg" },
            new ComponentType { Id = 29, Navn = "Dørautomatikk utstyr 4" },
            new ComponentType { Id = 30, Navn = "Dørautomatikk utstyr 5" },
            new ComponentType { Id = 31, Navn = "Kortleser inn" },
            new ComponentType { Id = 32, Navn = "Kortleser ut" },
            new ComponentType { Id = 33, Navn = "Kortleser styreenhet" },
            new ComponentType { Id = 34, Navn = "Impulsbryter innv" },
            new ComponentType { Id = 35, Navn = "Impulsbryter utv." },
            new ComponentType { Id = 36, Navn = "Impulsbryter utstyr" },
            new ComponentType { Id = 37, Navn = "Nøkkelbryter" },
            new ComponentType { Id = 38, Navn = "Nkl.bryter sylinder" },
            new ComponentType { Id = 39, Navn = "Dørlukker aktiv fløy" },
            new ComponentType { Id = 40, Navn = "Dørlukker arm/skinne" },
            new ComponentType { Id = 41, Navn = "Dørlukker passiv fløy" },
            new ComponentType { Id = 42, Navn = "Dørlukker utstyr" },
            new ComponentType { Id = 43, Navn = "Dørlukker utstyr 2" },
            new ComponentType { Id = 44, Navn = "Panikkbeslag/Skåte" },
            new ComponentType { Id = 45, Navn = "Panikkbeslag utstyr" },
            new ComponentType { Id = 46, Navn = "Panikkbeslag utstyr 2" },
            new ComponentType { Id = 47, Navn = "Panikkbeslag utstyr 3" },
            new ComponentType { Id = 48, Navn = "Panikkbeslag utstyr 4" },
            new ComponentType { Id = 49, Navn = "Panikkbeslag utstyr 5" },
            new ComponentType { Id = 50, Navn = "Magnetlås passiv fløy" },
            new ComponentType { Id = 51, Navn = "Magnetlås utstyr" },
            new ComponentType { Id = 52, Navn = "Magnetlås utstyr 2" },
            new ComponentType { Id = 53, Navn = "Nødutstyr mekanisk" },
            new ComponentType { Id = 54, Navn = "Nødutstyr elektrisk" },
            new ComponentType { Id = 55, Navn = "Karmoverføring aktiv fløy" },
            new ComponentType { Id = 56, Navn = "Karmoverføring passiv fløy" },
            new ComponentType { Id = 57, Navn = "Kabel" },
            new ComponentType { Id = 58, Navn = "Dørstopper" },
            new ComponentType { Id = 59, Navn = "Magnetkontakt" },
            new ComponentType { Id = 60, Navn = "Grensesnittboks" },
            new ComponentType { Id = 61, Navn = "Grensesnittboks utstyr 1" },
            new ComponentType { Id = 62, Navn = "Grensesnittboks utstyr 2" },
            new ComponentType { Id = 63, Navn = "Diverse 1" },
            new ComponentType { Id = 64, Navn = "Diverse 2" },
            new ComponentType { Id = 65, Navn = "Diverse 3" }
        );

        modelBuilder.Entity<DoorEnvironmentDocument>().HasData(
            new DoorEnvironmentDocument { Id = 1, Navn = "Dørmiljø 1", FileName = "dormiljo-1.pdf", Rekkefolge = 1 },
            new DoorEnvironmentDocument { Id = 2, Navn = "Dørmiljø 2", FileName = "dormiljo-2.pdf", Rekkefolge = 2 },
            new DoorEnvironmentDocument { Id = 3, Navn = "Dørmiljø 3", FileName = "dormiljo-3.pdf", Rekkefolge = 3 },
            new DoorEnvironmentDocument { Id = 4, Navn = "Dørmiljø 4", FileName = "dormiljo-4.pdf", Rekkefolge = 4 },
            new DoorEnvironmentDocument { Id = 5, Navn = "Dørmiljø 5", FileName = "dormiljo-5.pdf", Rekkefolge = 5 },
            new DoorEnvironmentDocument { Id = 6, Navn = "Dørmiljø 6", FileName = "dormiljo-6.pdf", Rekkefolge = 6 },
            new DoorEnvironmentDocument { Id = 7, Navn = "Dørmiljø 7", FileName = "dormiljo-7.pdf", Rekkefolge = 7 },
            new DoorEnvironmentDocument { Id = 8, Navn = "Dørmiljø 8", FileName = "dormiljo-8.pdf", Rekkefolge = 8 },
            new DoorEnvironmentDocument { Id = 9, Navn = "Dørmiljø 9", FileName = "dormiljo-9.pdf", Rekkefolge = 9 }
        );
    }
}
