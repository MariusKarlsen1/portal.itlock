using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Fase 3 serviceavtaler: serviceavtale som kobling på et overlevert prosjekt,
// og årlig avtaleverdi i hovedoversikten.
public class ServiceavtaleKoblingTests
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
    public async Task KobletProsjekt_BeholderStatusOgFinnesSomServiceavtale()
    {
        using var db = LagDb(out var c);
        db.Prosjekter.AddRange(
            new Prosjekt { Navn = "Overlevert med avtale", Status = ProsjektStatus.Overlevert, OverlevertDato = DateTime.Today, HarServiceavtale = true },
            new Prosjekt { Navn = "Overlevert uten avtale", Status = ProsjektStatus.Overlevert, OverlevertDato = DateTime.Today },
            new Prosjekt { Navn = "Gammel avtale", Status = ProsjektStatus.Serviceavtale });
        await db.SaveChangesAsync();

        var avtaler = await db.Prosjekter.Where(p => p.Status == ProsjektStatus.Serviceavtale || p.HarServiceavtale).Select(p => p.Navn).ToListAsync();
        Assert.Equal(["Overlevert med avtale", "Gammel avtale"], avtaler.OrderByDescending(n => n.Length).ToList());
        Assert.Equal(2, await db.Prosjekter.CountAsync(p => p.Status == ProsjektStatus.Overlevert));
    }

    [Fact]
    public async Task Arsverdier_BrukerGodkjentPrisFoerBeregnet()
    {
        using var db = LagDb(out var c);
        var godkjent = new Prosjekt { Navn = "A", Status = ProsjektStatus.Serviceavtale, ServiceGodkjentArspris = 12000, ServiceOppmote = 500, ServiceGangerPerAr = 2 };
        var beregnet = new Prosjekt { Navn = "B", Status = ProsjektStatus.Serviceavtale, ServiceOppmote = 500, ServiceGangerPerAr = 2 };
        db.Prosjekter.AddRange(godkjent, beregnet);
        await db.SaveChangesAsync();
        await ServiceRegelMotor.SikreStandardregler(db);

        var v = await ServiceRegelMotor.Arsverdier(db, [godkjent, beregnet]);
        Assert.Equal((12000m, true), v[godkjent.Id]);
        Assert.Equal((1000m, false), v[beregnet.Id]);
    }
}
