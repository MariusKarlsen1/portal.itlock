using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

// Malen for ordrebekreftelsen: standardteksten ligger inne, det som er endret
// lagres, og PDF-en følger malen.
public class OrdrebekreftelseMalTests
{
    [Fact]
    public void Uten_lagret_mal_brukes_standard()
    {
        var m = ObMalInnhold.Les(null);
        Assert.Contains(m.Felt, f => f.Nokkel == "kundenr" && f.Vis);
        Assert.Contains(m.Felt, f => f.Nokkel == "prosjektid" && !f.Vis);
        Assert.Equal(4, m.Punkter.Count);
        Assert.True(m.Punkter.Single(p => p.Tittel.StartsWith("Angrerett")).KunPrivat);
    }

    [Fact]
    public void Lagret_mal_beholdes_og_nye_standardfelt_legges_til()
    {
        var m = ObMalInnhold.Standard();
        m.Felt.RemoveAll(f => f.Nokkel == "ordre");
        m.FeltFor("betaling")!.Verdi = "14 dager netto";
        m.Punkter.Add(new ObMalPunkt("Garanti", "2 år") { Egen = true });
        var lest = ObMalInnhold.Les(m.Skriv());
        Assert.Equal("14 dager netto", lest.FeltFor("betaling")!.Verdi);
        Assert.NotNull(lest.FeltFor("ordre"));
        Assert.Contains(lest.Punkter, p => p.Egen && p.Tittel == "Garanti");
    }

    [Fact]
    public void Odelagt_json_gir_standard() => Assert.Equal(4, ObMalInnhold.Les("{ikke json").Punkter.Count);

    private sealed class Miljo : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PortalItlock.Web", "wwwroot"));
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "x";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Development";
    }

    private sealed class Tom : ITenantContext
    {
        public Tenant? Current => null;
    }

    [Fact]
    public async Task Eksempel_pdf_med_endret_mal()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        using var con = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        con.Open();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(con).Options);
        db.Database.EnsureCreated();
        var m = ObMalInnhold.Standard();
        m.FeltFor("levmate")!.Verdi = "Henting på lager";
        m.Punkter.Add(new ObMalPunkt("Garanti", "2 år på arbeid og varer.") { Egen = true });
        db.OrdrebekreftelseMaler.Add(new OrdrebekreftelseMal { Json = m.Skriv() });
        await db.SaveChangesAsync();
        var pdf = await new OrdrebekreftelsePdfService(db, new PdfLogo(new Miljo()), new Tom()).LagEksempelAsync(privat: false);
        Assert.True(pdf.Length > 1000);
        var ut = Environment.GetEnvironmentVariable("OB_EKSEMPEL");
        if (!string.IsNullOrEmpty(ut))
        {
            await File.WriteAllBytesAsync(ut, pdf);
        }
    }
}
