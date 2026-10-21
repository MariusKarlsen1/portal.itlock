using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Data;

// Katalogen over kunder (tenants) - hvilken SQLite-fil som hører til hvem.
// Egen database, egen fil, egne migrasjoner - helt adskilt fra kundenes egne
// ApplicationDbContext-filer. Kun plattformeier (deg) har tilgang hit.
public class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<PlattformBruker> PlattformBrukere => Set<PlattformBruker>();
    public DbSet<PlattformBrukerPasswordResetToken> PlattformBrukerPasswordResetTokener => Set<PlattformBrukerPasswordResetToken>();
    public DbSet<TenantLisens> TenantLisenser => Set<TenantLisens>();
    public DbSet<LisensPris> LisensPriser => Set<LisensPris>();
    public DbSet<TenantModul> TenantModuler => Set<TenantModul>();
    public DbSet<TenantInnstillinger> TenantInnstillinger => Set<TenantInnstillinger>();
    public DbSet<TenantVarselMottaker> TenantVarselMottakere => Set<TenantVarselMottaker>();
    public DbSet<TenantVarselRolle> TenantVarselRoller => Set<TenantVarselRolle>();
    public DbSet<TenantBrukerVarsel> TenantBrukerVarsler => Set<TenantBrukerVarsel>();
    public DbSet<PrisProdukt> PrisProdukter => Set<PrisProdukt>();
    public DbSet<TenantAvtalepris> TenantAvtalepriser => Set<TenantAvtalepris>();
    public DbSet<TenantAbonnement> TenantAbonnementer => Set<TenantAbonnement>();
    public DbSet<TenantAntallEndring> TenantAntallEndringer => Set<TenantAntallEndring>();
    public DbSet<PlattformFaktura> PlattformFakturaer => Set<PlattformFaktura>();
    public DbSet<PlattformFakturaLinje> PlattformFakturaLinjer => Set<PlattformFakturaLinje>();
    public DbSet<TenantNotat> TenantNotater => Set<TenantNotat>();
    public DbSet<TenantHendelse> TenantHendelser => Set<TenantHendelse>();
    public DbSet<TenantBackup> TenantBackuper => Set<TenantBackup>();
    public DbSet<PlattformInnstillinger> PlattformInnstillinger => Set<PlattformInnstillinger>();
    public DbSet<PrisPakke> PrisPakker => Set<PrisPakke>();
    public DbSet<PakkePris> PakkePriser => Set<PakkePris>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TenantModul>().HasIndex(m => new { m.TenantId, m.Modul }).IsUnique();
        modelBuilder.Entity<TenantInnstillinger>().HasIndex(i => i.TenantId).IsUnique();
        modelBuilder.Entity<TenantInnstillinger>().HasIndex(i => i.EpostNavn).IsUnique();
        modelBuilder.Entity<TenantVarselMottaker>().HasIndex(m => m.TenantId);
        modelBuilder.Entity<TenantVarselRolle>().HasIndex(v => new { v.TenantId, v.Nokkel, v.Rolle }).IsUnique();
        modelBuilder.Entity<TenantBrukerVarsel>().HasIndex(v => new { v.TenantId, v.BrukerId, v.Nokkel }).IsUnique();
        modelBuilder.Entity<PrisProdukt>().HasIndex(p => p.Nokkel).IsUnique();
        modelBuilder.Entity<TenantAvtalepris>().HasIndex(a => new { a.TenantId, a.Nokkel }).IsUnique();
        modelBuilder.Entity<TenantAbonnement>().HasIndex(a => a.TenantId).IsUnique();
        modelBuilder.Entity<TenantAntallEndring>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<TenantAntallEndring>().HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlattformFaktura>().HasIndex(f => new { f.TenantId, f.PeriodeFra });
        modelBuilder.Entity<PlattformFaktura>().HasMany(f => f.Linjer).WithOne(l => l.Faktura).HasForeignKey(l => l.FakturaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlattformFakturaLinje>().Ignore(l => l.Belop);
        modelBuilder.Entity<PlattformFaktura>().Ignore(f => f.ErForfalt);
        modelBuilder.Entity<TenantNotat>().HasIndex(n => n.TenantId);
        modelBuilder.Entity<TenantNotat>().HasOne<Tenant>().WithMany().HasForeignKey(n => n.TenantId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TenantHendelse>().HasIndex(h => h.TenantId);
        modelBuilder.Entity<PrisPakke>().HasIndex(p => p.Nokkel).IsUnique();
        modelBuilder.Entity<PakkePris>().HasIndex(p => new { p.PakkeNokkel, p.ProduktNokkel }).IsUnique();
        modelBuilder.Entity<TenantBackup>().HasIndex(b => new { b.TenantId, b.Tidspunkt });
        modelBuilder.Entity<TenantBackup>().HasOne(b => b.Tenant).WithMany().HasForeignKey(b => b.TenantId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<TenantHendelse>().HasOne<Tenant>().WithMany().HasForeignKey(h => h.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
