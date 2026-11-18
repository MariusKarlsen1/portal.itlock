using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Filene på varene ligger på disk ved siden av databasen, ikke inne i den.
public class KomponentFilLagerTests : IDisposable
{
    private readonly string _mappe = Directory.CreateTempSubdirectory("fk-filer-").FullName;
    private string Tilkobling => $"Data Source={Path.Combine(_mappe, "firma.db")};Pooling=False";
    private string FilMappe => KomponentFilLager.Mappe(Tilkobling)!;

    private ApplicationDbContext NyDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(Tilkobling).Options);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_mappe, recursive: true); } catch (IOException) { }
    }

    private async Task<string?> KolonneType(int id, string kolonne)
    {
        await using var con = new SqliteConnection(Tilkobling);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = $"SELECT typeof(\"{kolonne}\") FROM \"Components\" WHERE \"Id\" = {id}";
        return (string?)await cmd.ExecuteScalarAsync();
    }

    [Fact]
    public async Task Bilde_LagresPaDisk_LesesInnIgjen()
    {
        int id;
        using (var db = NyDb())
        {
            db.Database.EnsureCreated();
            var c = new Component { Navn = "Dørmodul 5-serien offline", BildeData = [1, 2, 3], BildeFilnavn = "a.png", BildeContentType = "image/png" };
            db.Components.Add(c);
            await db.SaveChangesAsync();
            id = c.Id;
            Assert.Equal([1, 2, 3], c.BildeData);
        }

        Assert.Equal("null", await KolonneType(id, "BildeData"));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(KomponentFilLager.Sti(FilMappe, id, "BildeData")));

        using (var db = NyDb())
        {
            var c = await db.LastFilerAsync(db.Components.FirstOrDefaultAsync(x => x.Id == id));
            Assert.Equal([1, 2, 3], c!.BildeData);
            Assert.Contains(id, await db.KomponenterMedFilAsync("BildeData"));
            Assert.Contains(id, await db.KomponenterMedFilAsync("BildeData", [id]));
            Assert.DoesNotContain(id, await db.KomponenterMedFilAsync("FdvData"));

            // Ingen endring = ingenting skrives.
            c.Navn = "Nytt navn";
            await db.SaveChangesAsync();
        }
        Assert.Equal("null", await KolonneType(id, "BildeData"));

        // Bytt bilde og legg til FDV, så fjern bildet.
        using (var db = NyDb())
        {
            var c = await db.LastFilerAsync(db.Components.FirstOrDefaultAsync(x => x.Id == id));
            c!.BildeData = [9, 9];
            c.FdvData = [7];
            await db.SaveChangesAsync();
        }
        Assert.Equal([9, 9], File.ReadAllBytes(KomponentFilLager.Sti(FilMappe, id, "BildeData")));
        Assert.Equal([7], File.ReadAllBytes(KomponentFilLager.Sti(FilMappe, id, "FdvData")));
        Assert.Equal("null", await KolonneType(id, "FdvData"));

        using (var db = NyDb())
        {
            var c = await db.LastFilerAsync(db.Components.FirstOrDefaultAsync(x => x.Id == id));
            Assert.Equal([7], c!.FdvData);
            c.BildeData = null;
            await db.SaveChangesAsync();
        }
        Assert.False(File.Exists(KomponentFilLager.Sti(FilMappe, id, "BildeData")));
        Assert.True(File.Exists(KomponentFilLager.Sti(FilMappe, id, "FdvData")));

        // Uten at filene er lastet: bildet legges til, FDV blir liggende.
        using (var db = NyDb())
        {
            var c = await db.Components.FirstAsync(x => x.Id == id);
            c.BildeData = [5];
            await db.SaveChangesAsync();
        }
        Assert.Equal([5], File.ReadAllBytes(KomponentFilLager.Sti(FilMappe, id, "BildeData")));
        Assert.Equal([7], File.ReadAllBytes(KomponentFilLager.Sti(FilMappe, id, "FdvData")));

        // Slett varen: filene forsvinner.
        using (var db = NyDb())
        {
            db.Components.Remove(await db.Components.FirstAsync(x => x.Id == id));
            await db.SaveChangesAsync();
        }
        Assert.False(Directory.Exists(Path.Combine(FilMappe, id.ToString())));
    }

    [Fact]
    public async Task Flytting_TarFilerUtAvDatabasen_BackupBakerDemInn()
    {
        int id;
        using (var db = NyDb())
        {
            db.Database.EnsureCreated();
            var c = new Component { Navn = "Gammel vare" };
            db.Components.Add(c);
            await db.SaveChangesAsync();
            id = c.Id;
            // Slik ligger det i dag: fila inne i databasen.
            await db.Database.ExecuteSqlRawAsync($"UPDATE \"Components\" SET \"BildeData\" = x'0A0B0C', \"DatabladData\" = x'01' WHERE \"Id\" = {id}");
        }

        Assert.Contains(id, await HarBilde());
        var flyttet = await KomponentFilFlytting.FlyttAsync(Tilkobling, NullLogger.Instance);
        Assert.Equal(2, flyttet);
        Assert.Equal("null", await KolonneType(id, "BildeData"));
        Assert.Equal([10, 11, 12], File.ReadAllBytes(KomponentFilLager.Sti(FilMappe, id, "BildeData")));
        Assert.Contains(id, await HarBilde());
        Assert.Equal(0, await KomponentFilFlytting.FlyttAsync(Tilkobling, NullLogger.Instance));

        using (var db = NyDb())
        {
            var c = await db.LastFilerAsync(db.Components.FirstOrDefaultAsync(x => x.Id == id));
            Assert.Equal([10, 11, 12], c!.BildeData);
            Assert.Equal([1], c.DatabladData);
        }

        // Backup: kopien får filene inn i seg igjen.
        var kopi = Path.Combine(_mappe, "kopi.db");
        File.Copy(Path.Combine(_mappe, "firma.db"), kopi);
        Assert.Equal(2, await KomponentFilLager.BakInnAsync(kopi, FilMappe));
        await using (var con = new SqliteConnection($"Data Source={kopi};Pooling=False"))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"SELECT length(\"BildeData\") FROM \"Components\" WHERE \"Id\" = {id}";
            Assert.Equal(3L, await cmd.ExecuteScalarAsync());
        }

        Assert.False(await KomponentFilFlytting.KrympAsync(Tilkobling, NullLogger.Instance));
    }

    private async Task<HashSet<int>> HarBilde()
    {
        using var db = NyDb();
        return await db.KomponenterMedFilAsync("BildeData");
    }

    [Fact]
    public void MinneDatabase_HarIngenMappe() => Assert.Null(KomponentFilLager.Mappe("Data Source=:memory:"));
}
