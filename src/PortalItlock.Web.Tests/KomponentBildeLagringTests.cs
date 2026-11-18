using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Varekortet: bilde lagt til på en vare uten filer fra før, og så Lagre.
public class KomponentBildeLagringTests
{
    [Fact]
    public async Task Bilde_PaVareUtenFiler_KanLagres()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conn).Options;
        int id;
        using (var db = new ApplicationDbContext(opts))
        {
            db.Database.EnsureCreated();
            var c = new Component { Navn = "Dørmodul 5-serien offline" };
            db.Components.Add(c);
            await db.SaveChangesAsync();
            id = c.Id;
        }

        using (var db = new ApplicationDbContext(opts))
        {
            var c = await db.Components.Include(x => x.Produktgrupper).Include(x => x.FdvDokumenter)
                .Include(x => x.MontasjebladDokumenter).Include(x => x.DatabladDokumenter).AsSplitQuery()
                .FirstAsync(x => x.Id == id);
            await db.LastFilerAsync(c);
            c.BildeData = [1, 2, 3];
            c.BildeFilnavn = "bilde.png";
            c.BildeContentType = "image/png";
            await db.SaveChangesAsync();
            c.Navn = c.Navn.Trim();
            await db.SaveChangesAsync();
        }

        using (var db = new ApplicationDbContext(opts))
        {
            var c = await db.Components.FirstAsync(x => x.Id == id);
            await db.LastFilerAsync(c);
            Assert.Equal([1, 2, 3], c.BildeData);
        }
    }
}
