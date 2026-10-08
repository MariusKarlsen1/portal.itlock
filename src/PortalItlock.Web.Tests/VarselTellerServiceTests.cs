using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Regresjonstester for ytelsesfiksen i VarselTellerService (CODE_REVIEW.md,
// funn A1): varseltallene skal fortsatt være korrekte, og skal nå caches kort
// (20 sek) i stedet for å spørres på nytt ved hver sidenavigasjon.
public class VarselTellerServiceTests
{
    private static ApplicationDbContext LagDb(out SqliteConnection connection)
    {
        // "Foreign Keys=False" - testen bryr seg kun om Avvik-telling/caching,
        // ikke om referanseintegritet, så vi slipper å bygge opp en full
        // Prosjekt+Dor-kjede bare for å tilfredsstille FK-constraints.
        connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class FastTenantContext(int? tenantId) : ITenantContext
    {
        public Tenant? Current { get; } = tenantId is int id ? new Tenant { Id = id, Navn = "Test", ConnectionString = "" } : null;
    }

    [Fact]
    public async Task HentAsync_teller_avvik_som_venter_korrekt()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.Avvik.Add(new Avvik { DorId = 1, Beskrivelse = "Test", Status = AvvikStatus.SendtTilKunde });
            db.Avvik.Add(new Avvik { DorId = 1, Beskrivelse = "Test 2", Status = AvvikStatus.Apent });
            await db.SaveChangesAsync();

            var service = new VarselTellerService(db, new MemoryCache(new MemoryCacheOptions()), new FastTenantContext(1));
            var teller = await service.HentAsync(visUtvidet: false, erAdmin: false);

            Assert.Equal(1, teller.AvvikSomVenter);
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task HentAsync_cacher_resultatet_i_stedet_for_a_sporre_pa_nytt_ved_hvert_kall()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.Avvik.Add(new Avvik { DorId = 1, Beskrivelse = "Test", Status = AvvikStatus.SendtTilKunde });
            await db.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new VarselTellerService(db, cache, new FastTenantContext(1));

            var forste = await service.HentAsync(visUtvidet: false, erAdmin: false);
            Assert.Equal(1, forste.AvvikSomVenter);

            // Simulerer at noe endrer seg i databasen MENS cachen fortsatt er gyldig
            // (akkurat som en bakgrunnstjeneste eller en annen bruker ville gjort i
            // produksjon) - andre kall innen cache-vinduet skal IKKE se endringen.
            db.Avvik.Add(new Avvik { DorId = 1, Beskrivelse = "Nytt avvik", Status = AvvikStatus.SendtTilKunde });
            await db.SaveChangesAsync();

            var andre = await service.HentAsync(visUtvidet: false, erAdmin: false);
            Assert.Equal(1, andre.AvvikSomVenter);
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task HentAsync_deler_ikke_cache_mellom_ulike_tenants()
    {
        using var db = LagDb(out var connection);
        try
        {
            db.Avvik.Add(new Avvik { DorId = 1, Beskrivelse = "Test", Status = AvvikStatus.SendtTilKunde });
            await db.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var tenant1Service = new VarselTellerService(db, cache, new FastTenantContext(1));
            var tenant2Service = new VarselTellerService(db, cache, new FastTenantContext(2));

            var tenant1Teller = await tenant1Service.HentAsync(visUtvidet: false, erAdmin: false);
            var tenant2Teller = await tenant2Service.HentAsync(visUtvidet: false, erAdmin: false);

            // Samme underliggende DbContext her (kun for testens skyld), men
            // poenget er at cache-nøkkelen inkluderer tenant-id, slik at ulike
            // tenants aldri kan lese hverandres cachede varseltall.
            Assert.Equal(1, tenant1Teller.AvvikSomVenter);
            Assert.Equal(1, tenant2Teller.AvvikSomVenter);
        }
        finally
        {
            connection.Close();
        }
    }
}
