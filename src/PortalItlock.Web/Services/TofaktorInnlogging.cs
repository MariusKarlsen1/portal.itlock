using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Cookiene i tofaktor-innloggingen. «Venter» holder hvem som har skrevet riktig
/// passord og venter på koden (10 minutter, kryptert og tidsbegrenset med
/// DataProtection). «Husk» betyr at enheten slipper koden i 30 dager; den gjelder
/// bare så lenge brukerens TofaktorStempel er det samme (byttes ved «glem enheter»
/// og når tofaktor slås av eller tilbakestilles).
/// </summary>
public static class TofaktorInnlogging
{
    public const string KravClaim = "TofaktorKrav";
    private const string VenterFormal = "FullKontroll.Tofaktor.Venter.v1";
    private const string HuskFormal = "FullKontroll.Tofaktor.Husk.v1";

    public sealed record Venter(int TenantId, int BrukerId, string ReturnUrl, int Feil);

    private static CookieOptions Valg(HttpContext http, TimeSpan varighet) => new()
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = varighet,
        IsEssential = true
    };

    public static void SettVenter(HttpContext http, IDataProtectionProvider vern, int tenantId, int brukerId, string returnUrl, int feil)
    {
        var beskytter = vern.CreateProtector(VenterFormal).ToTimeLimitedDataProtector();
        var trygg = returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") ? returnUrl : "/";
        var verdi = beskytter.Protect($"{tenantId}|{brukerId}|{feil}|{trygg}", TimeSpan.FromMinutes(10));
        http.Response.Cookies.Append(Tofaktor.VenterCookie, verdi, Valg(http, TimeSpan.FromMinutes(10)));
    }

    public static Venter? LesVenter(HttpContext http, IDataProtectionProvider vern)
    {
        if (!http.Request.Cookies.TryGetValue(Tofaktor.VenterCookie, out var verdi) || string.IsNullOrEmpty(verdi))
        {
            return null;
        }
        try
        {
            var deler = vern.CreateProtector(VenterFormal).ToTimeLimitedDataProtector().Unprotect(verdi).Split('|', 4);
            return new Venter(int.Parse(deler[0]), int.Parse(deler[1]), deler[3], int.Parse(deler[2]));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool HarVenter(HttpContext http, IDataProtectionProvider vern) => LesVenter(http, vern) is not null;

    public static void FjernVenter(HttpContext http) => http.Response.Cookies.Delete(Tofaktor.VenterCookie, new CookieOptions { Path = "/" });

    // Én husk-cookie per bruker og organisasjon, så flere kan dele en PC.
    private static string HuskNavn(int tenantId, int brukerId) => $"{Tofaktor.HuskCookie}_{tenantId}_{brukerId}";

    public static void Husk(HttpContext http, IDataProtectionProvider vern, int tenantId, Bruker bruker)
    {
        var beskytter = vern.CreateProtector(HuskFormal).ToTimeLimitedDataProtector();
        var verdi = beskytter.Protect($"{tenantId}|{bruker.Id}|{bruker.TofaktorStempel}", TimeSpan.FromDays(30));
        http.Response.Cookies.Append(HuskNavn(tenantId, bruker.Id), verdi, Valg(http, TimeSpan.FromDays(30)));
    }

    public static bool ErHusket(HttpContext http, IDataProtectionProvider vern, int tenantId, Bruker bruker)
    {
        if (!http.Request.Cookies.TryGetValue(HuskNavn(tenantId, bruker.Id), out var verdi) || string.IsNullOrEmpty(verdi))
        {
            return false;
        }
        try
        {
            var deler = vern.CreateProtector(HuskFormal).ToTimeLimitedDataProtector().Unprotect(verdi).Split('|', 3);
            return deler[0] == tenantId.ToString() && deler[1] == bruker.Id.ToString() && deler[2] == (bruker.TofaktorStempel ?? "");
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> ErKrevdAsync(string connectionString, BrukerRolle rolle)
    {
        try
        {
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options);
            return (await Tofaktor.KrevdeRollerAsync(db)).Contains(rolle);
        }
        catch (Exception)
        {
            // Skal aldri stoppe innloggingen.
            return false;
        }
    }
}
