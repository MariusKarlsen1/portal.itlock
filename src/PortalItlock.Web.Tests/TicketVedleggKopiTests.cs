using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

public class TicketVedleggKopiTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        connection.Open();
        SqliteUnicodeFunctionsInterceptor.RegistrerFunksjoner(connection);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static ForesporselMedia Media(int foresporselId, string navn, bool inline = false) => new()
    {
        ForesporselId = foresporselId,
        Filnavn = navn,
        ContentType = "image/png",
        Data = [1, 2, 3],
        ErInline = inline
    };

    [Fact]
    public async Task Kopierer_vedlegg_men_ikke_innebygde_bilder()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.ForesporselMedia.AddRange(Media(1, "bilde.png"), Media(1, "logo.png", inline: true), Media(2, "annen.png"));
            await db.SaveChangesAsync();

            var antall = await TicketVedleggKopi.KopierFraForesporselAsync(db, 1, 7);
            await db.SaveChangesAsync();

            Assert.Equal(1, antall);
            var kopi = Assert.Single(await db.TicketMedia.ToListAsync());
            Assert.Equal("bilde.png", kopi.Filnavn);
            Assert.Equal(7, kopi.TicketId);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Kopierer_ikke_samme_vedlegg_to_ganger()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.ForesporselMedia.Add(Media(1, "bilde.png"));
            await db.SaveChangesAsync();

            await TicketVedleggKopi.KopierFraForesporselAsync(db, 1, 7);
            await db.SaveChangesAsync();
            var andreGang = await TicketVedleggKopi.KopierFraForesporselAsync(db, 1, 7);

            Assert.Equal(0, andreGang);
        }
        finally
        {
            connection.Dispose();
        }
    }
}
