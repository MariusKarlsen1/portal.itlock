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
}
