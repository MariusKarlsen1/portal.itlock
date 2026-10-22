using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// ApplicationDbContext legger e-postvarsler i køen når en arbeidsordre får ny
// montør, når det kommer en forespørsel, og når det meldes avvik.
public class EpostVarselKoTests : IDisposable
{
    private readonly string _fil = Path.Combine(Path.GetTempPath(), $"varsel-{Guid.NewGuid():N}.db");

    private ApplicationDbContext LagDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={_fil};Foreign Keys=False")
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static List<EpostVarselKo.Hendelse> Tom(string tilkobling)
    {
        var liste = new List<EpostVarselKo.Hendelse>();
        while (EpostVarselKo.Leser.TryRead(out var h))
        {
            if (h.ConnectionString == tilkobling)
            {
                liste.Add(h);
            }
        }

        return liste;
    }

    [Fact]
    public async Task NyttAvvik_GirHendelseMedDenSomMeldte()
    {
        await using var db = LagDb();
        var tilkobling = db.Database.GetConnectionString()!;
        Tom(tilkobling);

        db.Avvik.Add(new Avvik { DorId = 1, Beskrivelse = "Dør går ikke i lås", OpprettetAvBrukerId = 7 });
        await db.SaveChangesAsync();

        var h = Assert.Single(Tom(tilkobling), x => x.Nokkel == EpostVarsler.AvvikNytt);
        Assert.Equal(7, h.BrukerId);
        Assert.True(h.Id > 0);
    }

    [Fact]
    public async Task NyMontorPaOrdre_GirHendelse_MenIkkeVedAnnenEndring()
    {
        await using var db = LagDb();
        var tilkobling = db.Database.GetConnectionString()!;
        var ordre = new Arbeidsordre { Tittel = "Bytte sylinder" };
        db.Arbeidsordre.Add(ordre);
        await db.SaveChangesAsync();
        Tom(tilkobling);

        ordre.AnsvarligMontorId = 3;
        await db.SaveChangesAsync();
        var h = Assert.Single(Tom(tilkobling), x => x.Nokkel == EpostVarsler.OrdreTildelt);
        Assert.Equal(3, h.BrukerId);

        ordre.Beskrivelse = "Ny tekst";
        await db.SaveChangesAsync();
        Assert.Empty(Tom(tilkobling));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_fil);
        }
        catch (IOException)
        {
        }
    }
}
