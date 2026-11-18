using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Samme plantegning lastet opp på nytt (også «Navn (2)» fra nedlastinger)
// blir revisjon 1: tegningen lagres som den er, og dørene står på samme plass.
public class PlantegningRevisjonTests
{
    [Fact]
    public async Task SammeNavn_MedKopinummer_BlirRevisjon_DorerStaarStille()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conn).Options;
        using var db = new ApplicationDbContext(opts);
        db.Database.EnsureCreated();
        var p = new Prosjekt { Navn = "Bygg A", Status = ProsjektStatus.Tilbud };
        db.Prosjekter.Add(p);
        await db.SaveChangesAsync();

        var forste = await PlantegningOpplasting.LagreAsync(db, p.Id, "A22-001-01 Tømrerplan 1. etasje, bygg A", null, "a.pdf", "image/png", [1, 2, 3]);
        db.Dorer.Add(new Dor { ProsjektId = p.Id, PlantegningId = forste.Plantegning.Id, Dornummer = "D-001", PosX = 45.8, PosY = 28.8 });
        await db.SaveChangesAsync();

        var ny = new byte[] { 9, 8, 7, 6 };
        var andre = await PlantegningOpplasting.LagreAsync(db, p.Id, "A22-001-01  tømrerplan 1. etasje, bygg A (2)", null, "a (2).pdf", "image/png", ny);

        Assert.True(andre.Revidert);
        Assert.Equal(forste.Plantegning.Id, andre.Plantegning.Id);
        Assert.Equal(1, andre.Plantegning.Revisjon);
        Assert.Equal("A22-001-01 Tømrerplan 1. etasje, bygg A", andre.Plantegning.Navn);
        Assert.Single(await db.Plantegninger.ToListAsync());

        db.ChangeTracker.Clear();
        var lagret = await db.Plantegninger.Include(x => x.Fil).FirstAsync();
        Assert.Equal(ny, lagret.Data);
        var dor = await db.Dorer.FirstAsync();
        Assert.Equal(lagret.Id, dor.PlantegningId);
        Assert.Equal(45.8, dor.PosX);
        Assert.Equal(28.8, dor.PosY);
        var rev = await db.PlantegningRevisjoner.FirstAsync();
        Assert.Equal(0, rev.Revisjon);
    }

    [Theory]
    [InlineData("Plan 1. etg (2)", "Plan 1. etg")]
    [InlineData("Plan 1. etg", "Plan 1. etg")]
    [InlineData("Plan (A) del 2", "Plan (A) del 2")]
    public void KopinummerFjernes(string inn, string ut) => Assert.Equal(ut, PlantegningOpplasting.UtenKopiNummer(inn));
}
