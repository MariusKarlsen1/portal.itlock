using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Ressursplan: ISO-uker over årsskiftet, og at prosjektene hentes inn og
// følger status (nye, tilbud, avslått, godkjent) uten å fylle Endringer første gang.
public class UkeplanServiceTests
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
    public void Uker_GaarOverArsskiftet_Med53Uker()
    {
        var uker = UkeplanService.Uker(new UkeplanService.Uke(2026, 52), 3);
        Assert.Equal([new(2026, 52), new(2026, 53), new(2027, 1)], uker);
        Assert.Equal(new UkeplanService.Uke(2026, 47), UkeplanService.Uke.For(new DateTime(2026, 11, 17)));
    }

    [Fact]
    public async Task Synk_HenterInnProsjekter_OgFolgerStatus()
    {
        using var db = LagDb(out var conn);
        using var _ = conn;
        var pagar = new Prosjekt { Navn = "Tangen 32", Status = ProsjektStatus.UnderArbeid };
        var tilbud = new Prosjekt { Navn = "Kvinesdal skole", Status = ProsjektStatus.Tilbud };
        var service = new Prosjekt { Navn = "SSHF", Status = ProsjektStatus.Serviceavtale };
        db.Prosjekter.AddRange(pagar, tilbud, service);
        await db.SaveChangesAsync();
        var svc = new UkeplanService(db);

        await svc.SynkAsync();
        var rader = await db.UkeplanRader.ToListAsync();
        Assert.Equal(2, rader.Count);
        Assert.Equal(UkeplanKategori.Mindre, rader.Single(r => r.ProsjektId == pagar.Id).Kategori);
        Assert.Equal(UkeplanKategori.Potensiell, rader.Single(r => r.ProsjektId == tilbud.Id).Kategori);
        Assert.Empty(await db.UkeplanEndringer.ToListAsync());

        // Nytt prosjekt, tilbudet godkjennes, og et annet avslås.
        var nytt = new Prosjekt { Navn = "Sokndal helsesenter", Status = ProsjektStatus.Registrert };
        var avslas = new Prosjekt { Navn = "Bryne senter", Status = ProsjektStatus.Tilbud };
        db.Prosjekter.AddRange(nytt, avslas);
        await db.SaveChangesAsync();
        await svc.SynkAsync();
        avslas.Status = ProsjektStatus.TilbudAvslatt;
        tilbud.Status = ProsjektStatus.UnderArbeid;
        await db.SaveChangesAsync();
        await svc.SynkAsync();

        rader = await db.UkeplanRader.ToListAsync();
        Assert.Equal(UkeplanKategori.Mindre, rader.Single(r => r.ProsjektId == tilbud.Id).Kategori);
        Assert.True(rader.Single(r => r.ProsjektId == avslas.Id).Skjult);
        var endringer = await db.UkeplanEndringer.OrderBy(e => e.Id).Select(e => e.Type).ToListAsync();
        Assert.Contains(UkeplanEndringType.Ny, endringer);
        Assert.Contains(UkeplanEndringType.Avslatt, endringer);
        Assert.Contains(UkeplanEndringType.Godkjent, endringer);

        // Kjøres igjen: ingen nye endringer.
        await svc.SynkAsync();
        Assert.Equal(endringer.Count, await db.UkeplanEndringer.CountAsync());
    }

    [Fact]
    public async Task Kalkulert_TarIkkeMedOpsjoner()
    {
        using var db = LagDb(out var conn);
        using var _ = conn;
        var p = new Prosjekt { Navn = "Bygg", Status = ProsjektStatus.UnderArbeid };
        db.Prosjekter.Add(p);
        await db.SaveChangesAsync();
        db.MonteringLinjer.AddRange(
            new MonteringLinje { ProsjektId = p.Id, Navn = "Sylinder", Antall = 10, Minutter = 30 },
            new MonteringLinje { ProsjektId = p.Id, Navn = "Dørlukker", Antall = 2, Minutter = 60, ErOpsjon = true });
        await db.SaveChangesAsync();

        var kalk = await new UkeplanService(db).KalkulertAsync([p.Id]);
        Assert.Equal(5m, kalk[p.Id]);
    }
}
