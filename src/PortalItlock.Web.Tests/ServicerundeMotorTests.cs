using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Fase 4 serviceavtaler: servicerunde dør for dør med øyeblikksbilde,
// kontrollpunkter fra servicetypene og aldri to pågående runder.
public class ServicerundeMotorTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return db;
    }

    private static List<ServiceKontrollpunkt> Mal() =>
    [
        new() { Servicetype = "Dørautomatikk", Tekst = "Funksjonstest", Rekkefolge = 0 },
        new() { Servicetype = "Dørautomatikk", Tekst = "Kontroll av mekanisk funksjon", Rekkefolge = 1 },
        new() { Servicetype = "Motorlås", Tekst = "Kontroll av mekanisk funksjon", Rekkefolge = 0 },
        new() { Servicetype = ServicerundeMotor.Generelt, Tekst = "Døren lukker og låser", Rekkefolge = 0 },
    ];

    [Fact]
    public void Punkter_SlarSammenLikePunkter()
    {
        var p = ServicerundeMotor.PunkterFor(["Motorlås", "Dørautomatikk"], Mal());
        Assert.Equal(2, p.Count);
        Assert.Equal("Dørautomatikk + Motorlås", p.Single(x => x.Tekst == "Kontroll av mekanisk funksjon").Kilde);
    }

    [Fact]
    public void DorUtenServicetyper_FarGenerellePunkter()
    {
        var p = ServicerundeMotor.PunkterFor([], Mal());
        Assert.Equal("Døren lukker og låser", Assert.Single(p).Tekst);
    }

    private static async Task<(Prosjekt P, Dor D, Component Gml, Component Ny)> LagAvtale(ApplicationDbContext db)
    {
        var type = new ComponentType { Navn = "Dørautomatikk" };
        var gml = new Component { Navn = "dormakaba ED100", Type = type };
        var ny = new Component { Navn = "dormakaba ED250", Type = type };
        var p = new Prosjekt { Navn = "Bygg B", Status = ProsjektStatus.Serviceavtale };
        var d = new Dor { Dornummer = "H-101", Prosjekt = p, Komponenter = [new DorKomponent { Component = gml, Montert = true }] };
        db.AddRange(type, gml, ny, p, d);
        await db.SaveChangesAsync();
        return (p, d, gml, ny);
    }

    [Fact]
    public async Task OpprettRunde_LagerOyeblikksbildeOgKontrollpunkter()
    {
        using var db = LagDb(out var c);
        var (p, _, _, _) = await LagAvtale(db);
        var id = await ServicerundeMotor.OpprettRunde(db, p.Id, null, DateTime.Today);

        var rd = await db.ServicerundeDorer.Include(d => d.Punkter).SingleAsync(d => d.ServicerundeId == id);
        Assert.Equal("H-101", rd.Dornummer);
        Assert.Contains(rd.Punkter, x => x.Tekst == "Funksjonstest åpne og lukke");
        Assert.Equal("dormakaba ED100", ServicerundeMotor.LesKomponenter(rd.KomponenterJson).Single().Navn);
        Assert.Equal(ServicerundeDorStatus.IkkeKontrollert, rd.Status);
    }

    [Fact]
    public async Task OpprettRundeToGanger_GirSammeRunde()
    {
        using var db = LagDb(out var c);
        var (p, _, _, _) = await LagAvtale(db);
        var a = await ServicerundeMotor.OpprettRunde(db, p.Id, null, DateTime.Today);
        var b = await ServicerundeMotor.OpprettRunde(db, p.Id, null, DateTime.Today);
        Assert.Equal(a, b);
        Assert.Equal(1, await db.Servicerunder.CountAsync());
    }

    [Fact]
    public async Task SenereBytte_EndrerIkkeRunden()
    {
        using var db = LagDb(out var c);
        var (p, d, gml, ny) = await LagAvtale(db);
        var id = await ServicerundeMotor.OpprettRunde(db, p.Id, null, DateTime.Today);

        var dk = await db.DorKomponenter.SingleAsync();
        db.DorKomponenter.Remove(dk);
        db.DorKomponenter.Add(new DorKomponent { DorId = d.Id, ComponentId = ny.Id, Montert = true });
        await db.SaveChangesAsync();

        var rd = await db.ServicerundeDorer.SingleAsync(x => x.ServicerundeId == id);
        Assert.Equal("dormakaba ED100", ServicerundeMotor.LesKomponenter(rd.KomponenterJson).Single().Navn);
    }

    [Fact]
    public async Task Fullfor_LagerAvvikEnGangOgLaserRunden()
    {
        using var db = LagDb(out var c);
        var (p, _, _, _) = await LagAvtale(db);
        p.ServiceGangerPerAr = 2;
        var id = await ServicerundeMotor.OpprettRunde(db, p.Id, null, new DateTime(2026, 11, 10));
        var runde = await db.Servicerunder.Include(r => r.Dorer).ThenInclude(d => d.Punkter).SingleAsync(r => r.Id == id);
        var d = runde.Dorer.Single();
        d.Punkter[0].Status = ServicerundeDorStatus.Avvik;
        d.Punkter[0].Kommentar = "Sensor reagerer sent";
        d.Status = ServicerundeMotor.BeregnStatus(d);

        var nye = await ServicerundeMotor.Fullfor(db, runde, null);
        var igjen = await ServicerundeMotor.Fullfor(db, runde, null);

        var avvik = Assert.Single(nye);
        Assert.Empty(igjen);
        Assert.Contains("Sensor reagerer sent", avvik.Beskrivelse);
        Assert.Equal(d.DorId, avvik.DorId);
        Assert.NotNull(runde.FullfortDato);
        Assert.Equal(new DateTime(2027, 5, 10), runde.NesteServiceDato);
        Assert.Null(await ServicerundeMotor.PagaendeRunde(db, p.Id));
    }

    [Fact]
    public async Task Fullfor_PlanleggerNesteRunde_OgNyRundeBrukerDenPlanlagte()
    {
        using var db = LagDb(out var c);
        var (p, _, _, _) = await LagAvtale(db);
        p.ServiceGangerPerAr = 12;
        var idag = DateTime.Today;
        var id = await ServicerundeMotor.OpprettRunde(db, p.Id, null, idag);
        var runde = await db.Servicerunder.Include(r => r.Dorer).ThenInclude(d => d.Punkter).SingleAsync(r => r.Id == id);
        await ServicerundeMotor.Fullfor(db, runde, null);
        await ServicerundeMotor.Fullfor(db, runde, null);

        var planlagt = await db.Servicerunder.SingleAsync(r => r.ErPlanlagt);
        Assert.Equal(idag.AddMonths(1), planlagt.Dato);

        var ny = await ServicerundeMotor.OpprettRunde(db, p.Id, null, idag.AddMonths(1));
        Assert.Equal(planlagt.Id, ny);
        Assert.Equal(2, await db.Servicerunder.CountAsync());
        Assert.False(await db.Servicerunder.AnyAsync(r => r.ErPlanlagt));
    }

    [Fact]
    public void Status_AvvikForanOk()
    {
        var d = new ServicerundeDor { Dornummer = "1", Punkter = [new() { Tekst = "a", Status = "ok" }, new() { Tekst = "b" }] };
        Assert.Equal(ServicerundeDorStatus.IkkeKontrollert, ServicerundeMotor.BeregnStatus(d));
        d.Punkter[1].Status = ServicerundeDorStatus.Avvik;
        Assert.Equal(ServicerundeDorStatus.Avvik, ServicerundeMotor.BeregnStatus(d));
        d.Punkter[1].Status = ServicerundeDorStatus.IkkeRelevant;
        Assert.Equal(ServicerundeDorStatus.Ok, ServicerundeMotor.BeregnStatus(d));
    }
}
