using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Tilbud serviceavtale: PDF-en lages med pristabell, tillegg og sum, med og uten valgene.
public class ServiceavtaleAntallPdfTests
{
    private sealed class FastTenant : ITenantContext
    {
        public Tenant? Current { get; } = new() { Id = 1, Navn = "Testfirma AS", ConnectionString = "" };
    }

    private sealed class TestMiljo(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Test";
    }

    [Fact]
    public async Task TilbudMedLinjerOgTillegg_GirPdf()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();

        var p = new Prosjekt
        {
            Navn = "Flekkefjord sykehjem", Adresse = "Kirkegata 12", Status = ProsjektStatus.Serviceavtale,
            ServicePrismodell = ServiceavtalePris.Antall, ServiceErTilbud = true, ServiceGangerPerAr = 2,
            Kunde = new Kunde { Navn = "Flekkefjord kommune" }
        };
        db.Prosjekter.Add(p);
        await db.SaveChangesAsync();
        var linjer = await ServiceavtaleAntall.LagLinjer(db, p.Id);
        linjer.First(l => l.Navn == "Dørautomatikk").Antall = 4;
        linjer.First(l => l.Navn == "Mekanisk").Antall = 12;
        linjer.Add(new ServiceavtaleLinje { ProsjektId = p.Id, ErTillegg = true, Navn = "Kjøring", TilleggType = ServiceavtaleLinje.PerKm, Pris = 8, Antall = 60, Rekkefolge = 99 });
        db.ServiceavtaleLinjer.AddRange(linjer);
        await db.SaveChangesAsync();

        var webRoot = Directory.CreateTempSubdirectory().FullName;
        await File.WriteAllTextAsync(Path.Combine(webRoot, "itlock-logo.svg"),
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\"/></svg>");
        var svc = new ServiceavtaleTilbudPdfService(db, new PdfLogo(new TestMiljo(webRoot)), new FastTenant());

        var standard = await svc.GenerateAsync(p.Id);
        p.ServiceTilbudValg = "linjer,stk,runde,mnd,signatur";
        p.ServiceRabattProsent = 10;
        await db.SaveChangesAsync();
        var alt = await svc.GenerateAsync(p.Id);
        p.ServiceTilbudValg = "";
        await db.SaveChangesAsync();
        var ingen = await svc.GenerateAsync(p.Id);

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(standard!, 0, 4));
        Assert.NotNull(alt);
        Assert.NotNull(ingen);
        var ut = Environment.GetEnvironmentVariable("SERVICE_PDF_UT");
        if (!string.IsNullOrEmpty(ut))
        {
            await File.WriteAllBytesAsync(ut, alt!);
        }
    }
}
