using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Fase 2 serviceavtaler: komponenthistorikken på dører skrives automatisk ved
// lagring (lagt til, montert, fjernet, bytte), og prisreglene versjoneres.
public class DorKomponentLoggTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return db;
    }

    private static (Dor Dor, Component A, Component B) LagDor(ApplicationDbContext db)
    {
        var p = new Prosjekt { Navn = "Bygg B" };
        var dor = new Dor { Dornummer = "D-12", Prosjekt = p };
        var a = new Component { Navn = "Effeff 118", Produktkode = "118E" };
        var b = new Component { Navn = "Effeff 331", Produktkode = "331" };
        db.AddRange(p, dor, a, b);
        db.SaveChanges();
        return (dor, a, b);
    }

    [Fact]
    public async Task LagtTilMontertOgFjernet_LoggesMedNavn()
    {
        using var db = LagDb(out var c);
        var (dor, a, _) = LagDor(db);

        db.DorKomponenter.Add(new DorKomponent { DorId = dor.Id, ComponentId = a.Id, Antall = 1 });
        await db.SaveChangesAsync();
        var dk = await db.DorKomponenter.SingleAsync();
        dk.Montert = true;
        dk.MontertDato = new DateTime(2025, 9, 14);
        await db.SaveChangesAsync();
        db.DorKomponenter.Remove(dk);
        await db.SaveChangesAsync();

        var logg = await db.DorKomponentLogger.OrderBy(l => l.Id).ToListAsync();
        Assert.Equal([DorKomponentHendelse.LagtTil, DorKomponentHendelse.Montert, DorKomponentHendelse.Fjernet], logg.Select(l => l.Hendelse));
        Assert.All(logg, l => Assert.Equal("Effeff 118", l.Navn));
        Assert.Equal(new DateTime(2025, 9, 14), logg[1].Tidspunkt);
    }

    [Fact]
    public async Task Bytte_BeholderGammelKomponentIHistorikken()
    {
        using var db = LagDb(out var c);
        var (dor, a, b) = LagDor(db);
        db.DorKomponenter.Add(new DorKomponent { DorId = dor.Id, ComponentId = a.Id, Antall = 1, Montert = true });
        await db.SaveChangesAsync();

        var gml = await db.DorKomponenter.SingleAsync();
        db.DorKomponenter.Remove(gml);
        db.DorKomponenter.Add(new DorKomponent { DorId = dor.Id, ComponentId = b.Id, Antall = 1, Montert = true });
        db.LoggMerknad = "Byttet Effeff 118 → Effeff 331";
        await db.SaveChangesAsync();

        var logg = await db.DorKomponentLogger.Where(l => l.Merknad != null).ToListAsync();
        Assert.Contains(logg, l => l.Hendelse == DorKomponentHendelse.Fjernet && l.Navn == "Effeff 118");
        Assert.Contains(logg, l => l.Hendelse == DorKomponentHendelse.LagtTil && l.Navn == "Effeff 331");
        Assert.Equal("Effeff 331", (await db.DorKomponenter.Include(k => k.Component).SingleAsync()).Component!.Navn);
    }

    [Fact]
    public async Task NyDorMedKomponent_FarRiktigDorId()
    {
        using var db = LagDb(out var c);
        var (_, a, _) = LagDor(db);
        var p = await db.Prosjekter.FirstAsync();
        var ny = new Dor { Dornummer = "D-13", ProsjektId = p.Id, Komponenter = [new DorKomponent { ComponentId = a.Id, Antall = 2 }] };
        db.Dorer.Add(ny);
        await db.SaveChangesAsync();

        var l = await db.DorKomponentLogger.SingleAsync();
        Assert.Equal(ny.Id, l.DorId);
        Assert.Equal(2, l.Antall);
    }

    [Fact]
    public async Task SlettetDor_LoggerIkkeOgFeilerIkke()
    {
        using var db = LagDb(out var c);
        var (dor, a, _) = LagDor(db);
        db.DorKomponenter.Add(new DorKomponent { DorId = dor.Id, ComponentId = a.Id });
        await db.SaveChangesAsync();
        var antallFor = await db.DorKomponentLogger.CountAsync();

        var d = await db.Dorer.Include(x => x.Komponenter).SingleAsync();
        db.Dorer.Remove(d);
        await db.SaveChangesAsync();

        Assert.Equal(antallFor, await db.DorKomponentLogger.CountAsync());
    }

    [Fact]
    public async Task Prisregler_Versjoneres()
    {
        using var db = LagDb(out var c);
        await ServiceRegelMotor.SikreStandardregler(db);
        Assert.Equal(1, await ServiceRegelMotor.GjeldendeVersjon(db));

        var r = await db.ServicePrisRegler.FirstAsync();
        r.Pris = 950;
        await db.SaveChangesAsync();
        Assert.Equal(2, await ServiceRegelMotor.LagreVersjon(db, "Test"));

        var v1 = ServiceRegelMotor.LesVersjon((await db.ServicePrisRegelVersjoner.SingleAsync(v => v.Versjon == 1)).Innhold);
        var v2 = ServiceRegelMotor.LesVersjon((await db.ServicePrisRegelVersjoner.SingleAsync(v => v.Versjon == 2)).Innhold);
        Assert.Equal(0, v1.First(x => x.Navn == r.Navn).Pris);
        Assert.Equal(950, v2.First(x => x.Navn == r.Navn).Pris);
    }
}
