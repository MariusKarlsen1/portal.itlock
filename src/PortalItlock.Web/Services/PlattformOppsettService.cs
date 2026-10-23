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

    // Tripletex har to adskilte miljøer: testmiljøet og ekte Tripletex
    // (produksjon). Hvert miljø har sitt eget consumer token og sine egne
    // ansattnøkler. Nøklene i serveroppsettet (Railway) gjelder miljøet
    // Tripletex:BaseUrl peker på (testmiljøet når den mangler), og brukes bare
    // når adminportalen ikke har noen for det miljøet.
    public TripletexNokler? PlattformTripletex() => TripletexFra(Hent(), Hent().TripletexTest);

    public TripletexNokler? PlattformTripletex(bool test) => TripletexFra(Hent(), test);

    public (string? Consumer, bool Test) ConsumerToken() => (ConsumerFor(Hent(), Hent().TripletexTest), Hent().TripletexTest);

    public string? ConsumerToken(bool test) => ConsumerFor(Hent(), test);

    private bool CfgTest => (config["Tripletex:BaseUrl"] ?? "api-test").Contains("api-test", StringComparison.OrdinalIgnoreCase);

    private string? CfgFor(string nokkel, bool test) =>
        CfgTest == test && config[$"Tripletex:{nokkel}"] is { Length: > 0 } verdi ? verdi : null;

    private string? ConsumerFor(PlattformInnstillinger inn, bool test) =>
        hemmelig.Dekrypter(test ? inn.TripletexTestConsumerTokenKryptert : inn.TripletexConsumerTokenKryptert) is { Length: > 0 } egen
            ? egen
            : CfgFor("ConsumerToken", test);

    private TripletexNokler? TripletexFra(PlattformInnstillinger inn, bool test)
    {
        var consumer = hemmelig.Dekrypter(test ? inn.TripletexTestConsumerTokenKryptert : inn.TripletexConsumerTokenKryptert);
        var employee = hemmelig.Dekrypter(test ? inn.TripletexTestEmployeeTokenKryptert : inn.TripletexEmployeeTokenKryptert);
        if (!string.IsNullOrWhiteSpace(consumer) && !string.IsNullOrWhiteSpace(employee))
        {
            return new TripletexNokler(consumer, employee, test, false);
        }

        var cfgConsumer = string.IsNullOrWhiteSpace(consumer) ? CfgFor("ConsumerToken", test) : consumer;
        var cfgEmployee = string.IsNullOrWhiteSpace(employee) ? CfgFor("EmployeeToken", test) : employee;
        return !string.IsNullOrWhiteSpace(cfgConsumer) && !string.IsNullOrWhiteSpace(cfgEmployee)
            ? new TripletexNokler(cfgConsumer, cfgEmployee, test, true)
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
    public async Task<TripletexNokler?> PlattformTripletexAsync()
    {
        var inn = await HentAsync();
        return TripletexFra(inn, inn.TripletexTest);
    }

    public async Task<TripletexNokler?> PlattformTripletexAsync(bool test) => TripletexFra(await HentAsync(), test);

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
