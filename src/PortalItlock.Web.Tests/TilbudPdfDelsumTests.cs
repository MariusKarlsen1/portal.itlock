using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Tilbud med overskrifter: PDF-en skal kunne lages med og uten delsum per
// overskrift, også når en overskrift står uten varer under.
public class TilbudPdfDelsumTests
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
    public async Task Delsum_PerOverskrift_GirPdf()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();

        var p = new Prosjekt { Navn = "Tangen", Status = ProsjektStatus.Tilbud, Kunde = new Kunde { Navn = "Kunde AS" } };
        var t = new Tilbud { Prosjekt = p, Tittel = "Lås & beslag", VisEnhetspris = true };
        t.Linjer.Add(new TilbudLinje { Navn = "Tangen 52", ErGruppering = true, Beskrivelse = "Utvendige dører", Rekkefolge = 0 });
        t.Linjer.Add(new TilbudLinje { Navn = "Sylinder", Utpris = 2650, Antall = 6, Rekkefolge = 1 });
        t.Linjer.Add(new TilbudLinje { Navn = "Låskasse", Utpris = 1180, Antall = 6, Rekkefolge = 2 });
        t.Linjer.Add(new TilbudLinje { Navn = "Tom overskrift", ErGruppering = true, Rekkefolge = 3 });
        t.Linjer.Add(new TilbudLinje { Navn = "Tangen 54", ErGruppering = true, Rekkefolge = 4 });
        t.Linjer.Add(new TilbudLinje { Navn = "Hengelås", Utpris = 3600, Antall = 1, Rekkefolge = 5 });
        db.Tilbud.Add(t);
        await db.SaveChangesAsync();

        var webRoot = Directory.CreateTempSubdirectory().FullName;
        await File.WriteAllTextAsync(Path.Combine(webRoot, "itlock-logo.svg"),
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\"/></svg>");
        var svc = new TilbudPdfService(db, new PdfLogo(new TestMiljo(webRoot)), new FastTenant());

        var uten = await svc.GenerateAsync(t.Id);
        t.VisDelsumPerOverskrift = true;
        await db.SaveChangesAsync();
        var med = await svc.GenerateAsync(t.Id);

        Assert.NotNull(uten);
        Assert.NotNull(med);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(med!, 0, 4));
        Assert.NotEqual(uten!.Length, med.Length);
    }
}
