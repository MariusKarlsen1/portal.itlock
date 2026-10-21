using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Krypterer nøkler (Tripletex, Resend) før de lagres i databasen, med
// AES-256-GCM. Hovednøkkelen kommer fra Plattform:Nokkel (base64, 32 byte),
// eller lages én gang og lagres i datamappen (plattform-nokkel.txt).
public class HemmelighetService(IConfiguration config)
{
    private byte[]? _nokkel;

    public bool NokkelFraOppsett => !string.IsNullOrWhiteSpace(config["Plattform:Nokkel"]);

    private byte[] Nokkel()
    {
        if (_nokkel is not null)
        {
            return _nokkel;
        }

        var fraOppsett = config["Plattform:Nokkel"];
        if (!string.IsNullOrWhiteSpace(fraOppsett))
        {
            return _nokkel = Convert.FromBase64String(fraOppsett.Trim());
        }

        var mappe = PlatformConnectionStringHelper.FinnDataMappe(config.GetConnectionString("DefaultConnection")!);
        var fil = Path.Combine(mappe, "plattform-nokkel.txt");
        if (File.Exists(fil))
        {
            return _nokkel = Convert.FromBase64String(File.ReadAllText(fil).Trim());
        }

        var ny = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(fil, Convert.ToBase64String(ny));
        return _nokkel = ny;
    }

    public string Krypter(string klartekst)
    {
        var data = Encoding.UTF8.GetBytes(klartekst);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var kryptert = new byte[data.Length];
        using var aes = new AesGcm(Nokkel(), 16);
        aes.Encrypt(nonce, data, kryptert, tag);
        return Convert.ToBase64String([.. nonce, .. tag, .. kryptert]);
    }

    public string? Dekrypter(string? lagret)
    {
        if (string.IsNullOrWhiteSpace(lagret))
        {
            return null;
        }

        try
        {
            var alt = Convert.FromBase64String(lagret);
            var klar = new byte[alt.Length - 28];
            using var aes = new AesGcm(Nokkel(), 16);
            aes.Decrypt(alt.AsSpan(0, 12), alt.AsSpan(28), alt.AsSpan(12, 16), klar);
            return Encoding.UTF8.GetString(klar);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        {
            return null;
        }
    }

    // Viser bare slutten av en nøkkel, f.eks. «••••••3f9a».
    public static string Skjult(string? verdi) =>
        string.IsNullOrEmpty(verdi) ? "" : "••••••" + (verdi.Length > 4 ? verdi[^4..] : "");
}

// Innstillingene for plattformen, med kort hurtiglager. Nøkler hentes
// herfra; mangler de i databasen, brukes serveroppsettet (miljøvariabler)
// som før, så ingenting slutter å virke før de er lagt inn i portalen.
public class PlattformOppsettService(IServiceScopeFactory scopeFactory, IMemoryCache cache, IConfiguration config, HemmelighetService hemmelig)
{
    private const string CacheNokkel = "plattform-innstillinger";

    public async Task<PlattformInnstillinger> HentAsync()
    {
        if (cache.TryGetValue(CacheNokkel, out PlattformInnstillinger? inn) && inn is not null)
        {
            return inn;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        inn = await db.PlattformInnstillinger.AsNoTracking().FirstOrDefaultAsync() ?? await OpprettAsync(db);
        cache.Set(CacheNokkel, inn, TimeSpan.FromSeconds(60));
        return inn;
    }

    // Synkron variant for steder som ikke kan vente (oppretting av tjenester).
    public PlattformInnstillinger Hent()
    {
        if (cache.TryGetValue(CacheNokkel, out PlattformInnstillinger? inn) && inn is not null)
        {
            return inn;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        inn = db.PlattformInnstillinger.AsNoTracking().FirstOrDefault();
        if (inn is null)
        {
            return new PlattformInnstillinger();
        }

        cache.Set(CacheNokkel, inn, TimeSpan.FromSeconds(60));
        return inn;
    }

    public TripletexNokler? PlattformTripletex() => TripletexFra(Hent());

    public (string? Consumer, bool Test) ConsumerToken()
    {
        var inn = Hent();
        var consumer = hemmelig.Dekrypter(inn.TripletexConsumerTokenKryptert);
        if (!string.IsNullOrWhiteSpace(consumer))
        {
            return (consumer, inn.TripletexTest);
        }

        var cfg = config["Tripletex:ConsumerToken"];
        return (string.IsNullOrWhiteSpace(cfg) ? null : cfg, CfgTest);
    }

    private bool CfgTest => (config["Tripletex:BaseUrl"] ?? "api-test").Contains("api-test", StringComparison.OrdinalIgnoreCase);

    private TripletexNokler? TripletexFra(PlattformInnstillinger inn)
    {
        var consumer = hemmelig.Dekrypter(inn.TripletexConsumerTokenKryptert);
        var employee = hemmelig.Dekrypter(inn.TripletexEmployeeTokenKryptert);
        if (!string.IsNullOrWhiteSpace(consumer) && !string.IsNullOrWhiteSpace(employee))
        {
            return new TripletexNokler(consumer, employee, inn.TripletexTest, false);
        }

        var cfgConsumer = config["Tripletex:ConsumerToken"];
        var cfgEmployee = config["Tripletex:EmployeeToken"];
        return !string.IsNullOrWhiteSpace(cfgConsumer) && !string.IsNullOrWhiteSpace(cfgEmployee)
            ? new TripletexNokler(cfgConsumer, cfgEmployee, CfgTest, true)
            : null;
    }

    public void Nullstill() => cache.Remove(CacheNokkel);

    private static async Task<PlattformInnstillinger> OpprettAsync(PlatformDbContext db)
    {
        var inn = new PlattformInnstillinger
        {
            Firmanavn = "Full Kontroll",
            VilkarTekst = Vilkar.StandardVilkar,
            DatabehandlerTekst = Vilkar.StandardDatabehandleravtale,
            TilbudIntro = Vilkar.StandardTilbudIntro
        };
        db.PlattformInnstillinger.Add(inn);
        await db.SaveChangesAsync();
        db.Entry(inn).State = EntityState.Detached;
        return inn;
    }

    public sealed record TripletexNokler(string ConsumerToken, string EmployeeToken, bool Test, bool FraServeroppsett);

    // Tripletex for Full Kontroll, der fakturaene til organisasjonene lages.
    public async Task<TripletexNokler?> PlattformTripletexAsync() => TripletexFra(await HentAsync());

    // Consumer-tokenet identifiserer Full Kontroll hos Tripletex, og brukes
    // for alle kunder sammen med kundens egen ansattnøkkel.
    public async Task<(string? Consumer, bool Test)> ConsumerTokenAsync()
    {
        var inn = await HentAsync();
        var consumer = hemmelig.Dekrypter(inn.TripletexConsumerTokenKryptert);
        if (!string.IsNullOrWhiteSpace(consumer))
        {
            return (consumer, inn.TripletexTest);
        }

        var cfg = config["Tripletex:ConsumerToken"];
        var test = (config["Tripletex:BaseUrl"] ?? "api-test").Contains("api-test", StringComparison.OrdinalIgnoreCase);
        return (string.IsNullOrWhiteSpace(cfg) ? null : cfg, test);
    }

    public async Task<string?> ResendNokkelAsync()
    {
        var inn = await HentAsync();
        return hemmelig.Dekrypter(inn.ResendApiKeyKryptert) is { Length: > 0 } nokkel ? nokkel : config["Resend:ApiKey"];
    }

    public async Task<string> StandardAvsenderAsync()
    {
        var inn = await HentAsync();
        return !string.IsNullOrWhiteSpace(inn.StandardAvsender) ? inn.StandardAvsender! : config["Resend:FraAdresse"] ?? "onboarding@resend.dev";
    }

    public static string TripletexBaseUrl(bool test) => test ? "https://api-test.tripletex.tech/v2/" : "https://tripletex.no/v2/";
}
