using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Regresjonstest for N+1-fiksen i DorDetalj.razor (CODE_REVIEW.md, "Bugs"):
// HentForFlereAsync skal returnere nøyaktig samme data som om HentAsync var
// kalt én gang per entityId i en løkke, bare i én spørring.
public class LagretPdfServiceTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task HentForFlereAsync_grupperer_korrekt_per_entityId_og_beholder_sortering()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.LagredePdfer.AddRange(
                new LagretPdf { EntityType = "Avvik", EntityId = 1, Navn = "Eldst", Data = [1], OpprettetDato = DateTime.Now.AddDays(-2) },
                new LagretPdf { EntityType = "Avvik", EntityId = 1, Navn = "Nyest", Data = [1], OpprettetDato = DateTime.Now },
                new LagretPdf { EntityType = "Avvik", EntityId = 2, Navn = "Avvik2-pdf", Data = [1] },
                // Annen entityType med samme id skal IKKE blandes inn.
                new LagretPdf { EntityType = "Tilbud", EntityId = 1, Navn = "Skal ikke med", Data = [1] });
            await db.SaveChangesAsync();

            var service = new LagretPdfService(db);
            var resultat = await service.HentForFlereAsync("Avvik", [1, 2, 3]);

            Assert.Equal(2, resultat.Count);
            Assert.False(resultat.ContainsKey(3));

            // Samme sortering (nyeste først) som HentAsync gir for avvik 1.
            Assert.Equal(["Nyest", "Eldst"], resultat[1].Select(p => p.Navn));
            Assert.Equal(["Avvik2-pdf"], resultat[2].Select(p => p.Navn));
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task HentForFlereAsync_gir_tom_dictionary_for_tom_idliste_uten_a_sporre_db()
    {
        using var db = LagDb(out var connection);
        try
        {
            var service = new LagretPdfService(db);
            var resultat = await service.HentForFlereAsync("Avvik", []);

            Assert.Empty(resultat);
        }
        finally
        {
            connection.Close();
        }
    }
}
