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
    }
}
