using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

// «Slett vare» i komponentregisteret feilet («An unhandled error has occurred»):
// ExecuteDelete går ikke på Components fordi filene deler tabellen.
public class KomponentSlettingTests
{
    [Fact]
    public async Task Vare_kan_slettes()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        var c = new Component { Navn = "TEST", Produktkode = "TEST-DS-01", PrisNetto = 20, PrisVeiledende = 59, Enhet = "stk" };
        db.Components.Add(c);
        await db.SaveChangesAsync();

        Assert.Equal(1, await KomponentSletting.SlettAsync(db, c.Id));
        Assert.False(await db.Components.AsNoTracking().AnyAsync(x => x.Id == c.Id));
    }
}
