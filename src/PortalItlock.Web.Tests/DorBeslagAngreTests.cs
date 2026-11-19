using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Beslag rett i lista på serviceavtalen: legg til med antall, fjern og angre.
public class DorBeslagAngreTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task LeggTilMedAntall_OgFjernMedAngre_GirSammeRaderTilbake()
    {
        using var db = LagDb(out var c);
        var p = new Prosjekt { Navn = "Pelagia" };
        var d1 = new Dor { Dornummer = "D1", Prosjekt = p };
        var d2 = new Dor { Dornummer = "D2", Prosjekt = p };
        var lukker = new Component { Navn = "ASSA DC700" };
        db.AddRange(p, d1, d2, lukker);
        await db.SaveChangesAsync();

        await DorBeslagEndring.LeggTil(db, [d1.Id, d2.Id], lukker.Id, 2);
        await DorBeslagEndring.LeggTil(db, [d1.Id], lukker.Id, 1);
        db.ChangeTracker.Clear();
        Assert.Equal(3, (await db.DorKomponenter.SingleAsync(k => k.DorId == d1.Id)).Antall);
        Assert.Equal(2, (await db.DorKomponenter.SingleAsync(k => k.DorId == d2.Id)).Antall);

        var kopi = await DorBeslagEndring.FjernMedKopi(db, [d1.Id, d2.Id], [lukker.Id]);
        Assert.Equal(2, kopi.Count);
        Assert.Empty(await db.DorKomponenter.ToListAsync());

        await DorBeslagEndring.Gjenopprett(db, kopi);
        db.ChangeTracker.Clear();
        var tilbake = await db.DorKomponenter.OrderBy(k => k.DorId).ToListAsync();
        Assert.Equal([3, 2], tilbake.Select(k => k.Antall));
    }

    [Fact]
    public async Task Gjenopprett_HopperOverDorSomErSlettet()
    {
        using var db = LagDb(out var c);
        var p = new Prosjekt { Navn = "Pelagia" };
        var d1 = new Dor { Dornummer = "D1", Prosjekt = p };
        var komp = new Component { Navn = "iLOQ S50" };
        db.AddRange(p, d1, komp);
        await db.SaveChangesAsync();
        await DorBeslagEndring.LeggTil(db, [d1.Id], komp.Id);
        var kopi = await DorBeslagEndring.FjernMedKopi(db, [d1.Id], [komp.Id]);

        db.Dorer.Remove(d1);
        await db.SaveChangesAsync();
        await DorBeslagEndring.Gjenopprett(db, kopi);

        Assert.Empty(await db.DorKomponenter.ToListAsync());
    }
}
