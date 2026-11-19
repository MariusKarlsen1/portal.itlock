using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Dupliser koblingsskjema: alt innhold følger med, prosjektkoblingen gjør ikke.
public class KoblingsSkjemaKopiTests
{
    [Fact]
    public async Task Kopi_HarSammeInnhold_MenIkkeProsjekt()
    {
        using var c = new SqliteConnection("Data Source=:memory:");
        c.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options);
        db.Database.EnsureCreated();
        var kat = new KoblingsKategori { Navn = "ADK" };
        var p = new Prosjekt { Navn = "Bygg A" };
        db.AddRange(kat, p);
        await db.SaveChangesAsync();
        var s = new KoblingsSkjema { Navn = "Salto", KategoriId = kat.Id, ProsjektId = p.Id, VisKomponentnavn = false };
        s.Symboler.Add(new KoblingsSymbol { ElementType = KoblingsElementType.Tekstboks, Tekst = "NO", Fet = true, SkjulNavn = true });
        s.Streker.Add(new KoblingsStrek { PunkterJson = "[{\"X\":1,\"Y\":2},{\"X\":3,\"Y\":2}]", TekstStart = "+12V" });
        db.Add(s);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var ny = await KoblingsSkjemaKopi.KopierAsync(db, s.Id);
        db.ChangeTracker.Clear();

        Assert.NotNull(ny);
        var lest = await db.KoblingsSkjemaer.Include(x => x.Symboler).Include(x => x.Streker).SingleAsync(x => x.Id == ny!.Id);
        Assert.Equal("Salto (kopi)", lest.Navn);
        Assert.Null(lest.ProsjektId);
        Assert.False(lest.VisKomponentnavn);
        var sym = Assert.Single(lest.Symboler);
        Assert.True(sym.Fet && sym.SkjulNavn);
        Assert.Equal("+12V", Assert.Single(lest.Streker).TekstStart);
        Assert.Equal(2, await db.KoblingsSymboler.CountAsync());
    }
}
