using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PortalItlock.Web.Services;

// Live API-integrasjon mot Tripletex (regnskapssystem). Skiller seg fra den
// eksisterende TripletexOrdreCsvService (manuell CSV-eksport man importerer
// selv inne i Tripletex) - denne snakker direkte med API-et.
//
// Autentisering er tre lag (se https://developer.tripletex.no):
// 1. Consumer-token (identifiserer appen) + employee-token (identifiserer
//    brukeren/firmaet) byttes inn mot et SESSION-token via
//    POST /token/session/:create.
// 2. Session-tokenet brukes deretter som PASSORD i vanlig HTTP Basic Auth
//    på alle andre kall, med brukernavn "0" (eget firma).
// 3. Session-tokenet varer til midnatt (CET) på angitt utløpsdato - hentes
//    derfor kun på nytt når det mangler eller snart utløper, ikke for hvert
//    kall.
//
// Registrert som singleton (se Program.cs) siden session-tokenet er delt
// for hele appen, ikke pr. bruker/forespørsel - _sessionLas hindrer at flere
// samtidige forespørsler lager session-tokenet flere ganger.
public sealed class TripletexService(HttpClient http, IOptions<TripletexOptions> options)
{
    private readonly TripletexOptions _options = options.Value;
    private readonly SemaphoreSlim _sessionLas = new(1, 1);
    private string? _sessionToken;
    private DateTime _sessionUtlopUtc = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public bool ErKonfigurert =>
        !string.IsNullOrWhiteSpace(_options.ConsumerToken) && !string.IsNullOrWhiteSpace(_options.EmployeeToken);

    private async Task<string> HentSessionTokenAsync(CancellationToken ct)
    {
        if (_sessionToken is not null && DateTime.UtcNow < _sessionUtlopUtc)
        {
            return _sessionToken;
        }

        await _sessionLas.WaitAsync(ct);
        try
        {
            // Sjekk på nytt - en annen forespørsel kan ha rukket å opprette
            // et gyldig token mens vi ventet på låsen.
            if (_sessionToken is not null && DateTime.UtcNow < _sessionUtlopUtc)
            {
                return _sessionToken;
            }

            if (!ErKonfigurert)
            {
                throw new InvalidOperationException(
                    "Tripletex er ikke konfigurert - mangler ConsumerToken/EmployeeToken (sett via user-secrets lokalt, eller Tripletex__ConsumerToken/Tripletex__EmployeeToken som miljøvariabler i prod).");
            }

            // Be om et session-token som er gyldig i 90 dager fremover - det
            // koster ingenting å be om lenger varighet, og sparer unødvendige
            // nye :create-kall. Fornyes automatisk før det faktisk utløper
            // (sjekken over, med litt margin - se _sessionUtlopUtc under).
            var utlopsdato = DateTime.UtcNow.AddDays(90).ToString("yyyy-MM-dd");
            var body = new
            {
                consumerToken = _options.ConsumerToken,
                employeeToken = _options.EmployeeToken,
                expirationDate = utlopsdato
            };

            using var resp = await http.PostAsJsonAsync("/token/session/:create", body, JsonOpts, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Tripletex avviste innlogging ({(int)resp.StatusCode}): {raw}");
            }

            var parsed = JsonSerializer.Deserialize<ResponseWrapper<SessionTokenSvar>>(raw, JsonOpts)
                ?? throw new InvalidOperationException("Tomt svar fra Tripletex ved oppretting av session-token.");

            var token = parsed.Value?.Token
                ?? throw new InvalidOperationException("Tripletex-svaret manglet selve token-verdien.");

            _sessionToken = token;
            // Litt margin (1 dag) i forhold til den faktiske utløpsdatoen vi ba om.
            _sessionUtlopUtc = DateTime.UtcNow.AddDays(89);
            return token;
        }
        finally
        {
            _sessionLas.Release();
        }
    }

    private async Task<HttpRequestMessage> LagAutorisertForespurselAsync(HttpMethod metode, string sti, CancellationToken ct)
    {
        var sessionToken = await HentSessionTokenAsync(ct);
        var req = new HttpRequestMessage(metode, sti);
        var basicVerdi = Convert.ToBase64String(Encoding.UTF8.GetBytes($"0:{sessionToken}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicVerdi);
        return req;
    }

    public sealed record TilkoblingResultat(bool Ok, string? Feilmelding, string? FirmaNavn, string? BrukerNavn);

    public async Task<TilkoblingResultat> TestTilkoblingAsync(CancellationToken ct = default)
    {
        try
        {
            // Tripletex ekspanderer IKKE nøstede referanser (employee/company)
            // som standard - uten "fields" kommer de bare tilbake som {id, url},
            // og navnet ville alltid blitt tomt.
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get,
                "/token/session/>whoAmI?fields=employee(firstName,lastName),company(name)", ct);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return new TilkoblingResultat(false, $"Tripletex svarte {(int)resp.StatusCode}: {raw}", null, null);
            }

            var parsed = JsonSerializer.Deserialize<ResponseWrapper<LoggedInUserInfoSvar>>(raw, JsonOpts);
            var navn = parsed?.Value?.Employee is { } e ? $"{e.FirstName} {e.LastName}".Trim() : null;
            var firma = parsed?.Value?.Company?.Name;
            return new TilkoblingResultat(true, null, firma, navn);
        }
        catch (Exception ex)
        {
            return new TilkoblingResultat(false, ex.Message, null, null);
        }
    }

    public sealed record TripletexKunde(int Id, string Navn, string? CustomerNumber, string? OrganizationNumber, string? Email);

    public async Task<(List<TripletexKunde> Kunder, string? Feilmelding)> SokKunderAsync(string navn, CancellationToken ct = default)
    {
        try
        {
            var query = string.IsNullOrWhiteSpace(navn) ? "" : $"?customerName={Uri.EscapeDataString(navn)}&count=25";
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, $"/customer{query}", ct);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return ([], $"Tripletex svarte {(int)resp.StatusCode}: {raw}");
            }

            var parsed = JsonSerializer.Deserialize<ListResponse<CustomerSvar>>(raw, JsonOpts);
            var kunder = (parsed?.Values ?? [])
                .Select(k => new TripletexKunde(k.Id, k.Name ?? "(uten navn)", k.CustomerNumber, k.OrganizationNumber, k.Email))
                .ToList();
            return (kunder, null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    // --- Interne DTO-er for deserialisering (kun feltene vi faktisk bruker) ---

    private sealed class ResponseWrapper<T>
    {
        public T? Value { get; set; }
    }

    private sealed class ListResponse<T>
    {
        public List<T>? Values { get; set; }
    }

    private sealed class SessionTokenSvar
    {
        public string? Token { get; set; }
    }

    private sealed class LoggedInUserInfoSvar
    {
        public EmployeeSvar? Employee { get; set; }
        public CompanySvar? Company { get; set; }
    }

    private sealed class EmployeeSvar
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    private sealed class CompanySvar
    {
        public string? Name { get; set; }
    }

    private sealed class CustomerSvar
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CustomerNumber { get; set; }
        public string? OrganizationNumber { get; set; }
        public string? Email { get; set; }
    }
}
