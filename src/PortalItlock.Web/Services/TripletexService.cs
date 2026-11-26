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
// Hver organisasjon bruker sin egen Tripletex: Full Kontroll sitt
// consumer-token sammen med organisasjonens egen ansattnøkkel, lagt inn
// kryptert i adminportalen (se TripletexNokkelOppslag). Har organisasjonen
// ingen nøkkel, er Tripletex stengt for den. Session-token og fakturacache
// holdes adskilt per nøkkel og per organisasjon.
public sealed class TripletexService(HttpClient http, TripletexService.Nokler? nokler, int tenantId)
{
    public sealed record Nokler(string ConsumerToken, string EmployeeToken, bool Test)
    {
        // API-nøkkel fra Selskap → API-nøkler i Tripletex («egne integrasjoner»):
        // én nøkkel som starter med tlxr_ og dekker både applikasjon og bruker.
        // Den trenger ikke Full Kontroll sitt consumer-token.
        public bool ErApiNokkel => ErApiNokkelVerdi(EmployeeToken);
    }

    public static bool ErApiNokkelVerdi(string? verdi) =>
        verdi?.Trim().StartsWith("tlxr_", StringComparison.Ordinal) == true;

    // Sesjonen fra en API-nøkkel varer maks 8 timer (ttlSeconds 300–28800).
    private const int ApiNokkelSesjonSekunder = 28800;

    private static readonly SemaphoreSlim _sessionLas = new(1, 1);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Token, DateTime UtlopUtc)> _sessioner = new();

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // SalgPerKunde.razor og Fakturaoversikt.razor hentet tidligere live fra
    // Tripletex ved HVER sideåpning (opptil 20s timeout, ingen fallback hvis
    // Tripletex er treg/nede) - se CODE_REVIEW.md 2026-10-08. Siden
    // fakturering skjer manuelt INNE i Tripletex (ikke i denne portalen),
    // trenger ikke tallene være sekundfriske - en kort cache her gjør begge
    // sidene umiddelbare ved gjentatt besøk/navigasjon innen samme vindu.
    private static readonly TimeSpan FakturaCacheLevetid = TimeSpan.FromMinutes(5);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime HentetUtc, List<TripletexFaktura> Fakturaer, string? Feilmelding)> _fakturaCache = new();

    public const string IkkeSattOpp = "Tripletex er ikke satt opp for denne organisasjonen ennå.";

    public bool ErTilgjengeligForOrganisasjonen => nokler is not null;

    public bool ErKonfigurert => nokler is not null;

    private string SesjonsNokkel => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        Encoding.UTF8.GetBytes($"{nokler!.ConsumerToken}|{nokler.EmployeeToken}|{nokler.Test}")));

    private async Task<string> HentSessionTokenAsync(CancellationToken ct)
    {
        // Sperren må komme før noe delt brukes.
        if (nokler is null)
        {
            throw new InvalidOperationException(IkkeSattOpp);
        }

        var nokkel = SesjonsNokkel;
        if (_sessioner.TryGetValue(nokkel, out var s) && DateTime.UtcNow < s.UtlopUtc)
        {
            return s.Token;
        }

        await _sessionLas.WaitAsync(ct);
        try
        {
            // Sjekk på nytt - en annen forespørsel kan ha rukket å opprette
            // et gyldig token mens vi ventet på låsen.
            if (_sessioner.TryGetValue(nokkel, out s) && DateTime.UtcNow < s.UtlopUtc)
            {
                return s.Token;
            }

            // API-nøkkel (tlxr_): byttes inn mot en sesjon på 8 timer via
            // :createFromRefreshToken. Fornyes 10 minutter før den går ut.
            if (nokler.ErApiNokkel)
            {
                var refreshBody = new { refreshToken = nokler.EmployeeToken.Trim(), ttlSeconds = ApiNokkelSesjonSekunder };
                using var refreshResp = await http.PostAsJsonAsync("token/session/:createFromRefreshToken", refreshBody, JsonOpts, ct);
                var refreshRaw = await refreshResp.Content.ReadAsStringAsync(ct);
                if (!refreshResp.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Tripletex avviste API-nøkkelen ({(int)refreshResp.StatusCode}): {refreshRaw}");
                }

                var refreshToken = JsonSerializer.Deserialize<ResponseWrapper<SessionTokenSvar>>(refreshRaw, JsonOpts)?.Value?.Token
                    ?? throw new InvalidOperationException("Tripletex-svaret manglet selve token-verdien.");
                _sessioner[nokkel] = (refreshToken, DateTime.UtcNow.AddSeconds(ApiNokkelSesjonSekunder - 600));
                return refreshToken;
            }

            // Be om et session-token som er gyldig i 90 dager fremover - det
            // koster ingenting å be om lenger varighet, og sparer unødvendige
            // nye :create-kall. Fornyes automatisk før det faktisk utløper.
            var utlopsdato = DateTime.UtcNow.AddDays(90).ToString("yyyy-MM-dd");
            var body = new
            {
                consumerToken = nokler.ConsumerToken,
                employeeToken = nokler.EmployeeToken,
                expirationDate = utlopsdato
            };

            // MERK: ingen ledende skråstrek - HttpClient sin BaseAddress har
            // en sti (".../v2/"), og en relativ URI som starter med "/" blir
            // tolket som absolutt fra host-roten, som DROPPER "/v2"-delen.
            using var resp = await http.PostAsJsonAsync("token/session/:create", body, JsonOpts, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Tripletex avviste innlogging ({(int)resp.StatusCode}): {raw}");
            }

            var parsed = JsonSerializer.Deserialize<ResponseWrapper<SessionTokenSvar>>(raw, JsonOpts)
                ?? throw new InvalidOperationException("Tomt svar fra Tripletex ved oppretting av session-token.");

            var token = parsed.Value?.Token
                ?? throw new InvalidOperationException("Tripletex-svaret manglet selve token-verdien.");

            // Litt margin (1 dag) i forhold til den faktiske utløpsdatoen vi ba om.
            _sessioner[nokkel] = (token, DateTime.UtcNow.AddDays(89));
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

    // Tripletex gir maks 1000 rader per kall (count). Lister hentes derfor side for
    // side med «from» til en side kommer tilbake med færre enn 1000 rader. Feiler en
    // side, returneres feilen og ingen rader, som før.
    private const int SideStorrelse = 1000;

    private async Task<(List<T> Verdier, string? Feilmelding)> HentAlleSiderAsync<T>(string sti, CancellationToken ct, int maksSider = 100)
    {
        var alle = new List<T>();
        var skille = sti.Contains('?') ? '&' : '?';
        for (var side = 0; side < maksSider; side++)
        {
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, $"{sti}{skille}from={side * SideStorrelse}&count={SideStorrelse}", ct);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return ([], $"Tripletex svarte {(int)resp.StatusCode}: {raw}");
            }

            var verdier = JsonSerializer.Deserialize<ListResponse<T>>(raw, JsonOpts)?.Values ?? [];
            alle.AddRange(verdier);
            if (verdier.Count < SideStorrelse)
            {
                break;
            }
        }
        return (alle, null);
    }

    // Utgående mva, høy sats (25 %) slås opp i firmaets egne mva-koder: koden må
    // være aktiv i kontoen. Faller tilbake til id 3, som er standardkoden.
    private int? _utgaendeMvaId;

    private async Task<int> UtgaendeMvaIdAsync(CancellationToken ct)
    {
        if (_utgaendeMvaId is int kjent)
        {
            return kjent;
        }

        try
        {
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, "ledger/vatType?typeOfVat=OUTGOING&number=3&fields=id,number,percentage", ct);
            using var resp = await http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                var treff = JsonSerializer.Deserialize<ListResponse<VatTypeSvar>>(await resp.Content.ReadAsStringAsync(ct), JsonOpts)?.Values?
                    .FirstOrDefault(v => v.Number == "3" && v.Percentage == 25m);
                if (treff is not null)
                {
                    _utgaendeMvaId = treff.Id;
                    return treff.Id;
                }
            }
        }
        catch (Exception)
        {
        }

        _utgaendeMvaId = 3;
        return 3;
    }

    public sealed record TilkoblingResultat(bool Ok, string? Feilmelding, string? FirmaNavn, string? BrukerNavn);

    // «The employee token cannot be used for this consumer and consumer token»: ansattnøkkelen
    // er laget for en annen applikasjon enn consumer-tokenet.
    public static bool ErFeilApplikasjon(string? feilmelding) =>
        feilmelding?.Contains("cannot be used for this consumer", StringComparison.OrdinalIgnoreCase) == true;

    // Hvilken Tripletex-konto nøklene går til, f.eks. «test:93305273». Null hvis Tripletex ikke svarer.
    public async Task<string?> KontoAsync(CancellationToken ct = default)
    {
        if (nokler is null)
        {
            return null;
        }
        try
        {
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, "token/session/>whoAmI?fields=companyId", ct);
            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            return doc.RootElement.TryGetProperty("value", out var v) && v.TryGetProperty("companyId", out var id) && id.ValueKind == JsonValueKind.Number
                ? $"{(nokler.Test ? "test" : "prod")}:{id.GetInt64()}"
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<TilkoblingResultat> TestTilkoblingAsync(CancellationToken ct = default)
    {
        try
        {
            // Tripletex ekspanderer IKKE nøstede referanser (employee/company)
            // som standard - uten "fields" kommer de bare tilbake som {id, url},
            // og navnet ville alltid blitt tomt.
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get,
                "token/session/>whoAmI?fields=companyId,employee(firstName,lastName),company(name)", ct);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return new TilkoblingResultat(false, $"Tripletex svarte {(int)resp.StatusCode}: {raw}", null, null);
            }

            var parsed = JsonSerializer.Deserialize<ResponseWrapper<LoggedInUserInfoSvar>>(raw, JsonOpts);
            var navn = parsed?.Value?.Employee is { } e ? $"{e.FirstName} {e.LastName}".Trim() : null;
            // Kontonummeret (contextId) med, så man ser hvilken Tripletex-konto
            // nøklene går til når flere kontoer har samme firmanavn.
            var firma = parsed?.Value?.Company?.Name is { } fn
                ? (parsed.Value.CompanyId is { } cid ? $"{fn} (konto {cid})" : fn)
                : null;
            return new TilkoblingResultat(true, null, firma, navn);
        }
        catch (Exception ex)
        {
            return new TilkoblingResultat(false, ex.Message, null, null);
        }
    }

    public sealed record TripletexKunde(
        int Id, string Navn, string? CustomerNumber, string? OrganizationNumber, string? Email,
        string? Telefon, string? Adresse, string? Postnr, string? Sted);

    public async Task<(List<TripletexKunde> Kunder, string? Feilmelding)> SokKunderAsync(string navn, CancellationToken ct = default)
    {
        try
        {
            var query = string.IsNullOrWhiteSpace(navn) ? "" : $"?customerName={Uri.EscapeDataString(navn)}&count=25";
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, $"customer{query}", ct);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return ([], $"Tripletex svarte {(int)resp.StatusCode}: {raw}");
            }

            var parsed = JsonSerializer.Deserialize<ListResponse<CustomerSvar>>(raw, JsonOpts);
            var kunder = (parsed?.Values ?? []).Select(TilTripletexKunde).ToList();
            return (kunder, null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    // Henter kunder som er opprettet/endret i Tripletex siden et gitt tidspunkt
    // (eller alle, hvis null) - brukes av TripletexSyncService for å oppdage
    // kunder opprettet direkte i Tripletex sitt eget grensesnitt, ikke via
    // portalen. "fields" ber uttrykkelig om adressefeltene, som ellers ikke
    // ekspanderes (samme fallgruve som whoAmI, se TestTilkoblingAsync).
    public async Task<(List<TripletexKunde> Kunder, string? Feilmelding)> HentEndredeKunderAsync(DateTime? endretSiden, CancellationToken ct = default)
    {
        try
        {
            var sti = "customer?fields=id,name,customerNumber,organizationNumber,email,phoneNumber,phoneNumberMobile,postalAddress(addressLine1,postalCode,city)";
            if (endretSiden is not null)
            {
                sti += $"&changedSince={Uri.EscapeDataString(endretSiden.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"))}";
            }

            var (verdier, feil) = await HentAlleSiderAsync<CustomerSvar>(sti, ct);
            return feil is not null ? ([], feil) : (verdier.Select(TilTripletexKunde).ToList(), null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    private static TripletexKunde TilTripletexKunde(CustomerSvar k) => new(
        k.Id, k.Name ?? "(uten navn)", k.CustomerNumber, k.OrganizationNumber, k.Email,
        k.PhoneNumberMobile ?? k.PhoneNumber,
        k.PostalAddress?.AddressLine1, k.PostalAddress?.PostalCode, k.PostalAddress?.City);

    // Kunden med dette org.nr. i Tripletex, om den finnes. Brukes før en ny
    // portalkunde opprettes der, så samme firma ikke blir to kunder.
    public async Task<TripletexKunde?> FinnKundeMedOrgNrAsync(string orgNr, CancellationToken ct = default)
    {
        try
        {
            var sti = $"customer?organizationNumber={Uri.EscapeDataString(orgNr)}&count=5&fields=id,name,customerNumber,organizationNumber,email,phoneNumber,phoneNumberMobile,postalAddress(addressLine1,postalCode,city)";
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, sti, ct);
            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            var parsed = JsonSerializer.Deserialize<ListResponse<CustomerSvar>>(await resp.Content.ReadAsStringAsync(ct), JsonOpts);
            return (parsed?.Values ?? []).Select(TilTripletexKunde)
                .FirstOrDefault(k => string.Equals(k.OrganizationNumber?.Replace(" ", ""), orgNr.Replace(" ", ""), StringComparison.Ordinal));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public sealed record TripletexKontakt(int Id, string Navn, string? Epost, string? Telefon);

    // Kontaktpersonene på en kunde i Tripletex.
    public async Task<(List<TripletexKontakt> Kontakter, string? Feilmelding)> HentKontakterAsync(int kundeId, CancellationToken ct = default)
    {
        try
        {
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get,
                $"contact?customerId={kundeId}&count=200&fields=id,firstName,lastName,email,phoneNumberMobile,phoneNumberWork", ct);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return ([], $"Tripletex svarte {(int)resp.StatusCode}: {raw}");
            }
            using var doc = JsonDocument.Parse(raw);
            var liste = new List<TripletexKontakt>();
            if (doc.RootElement.TryGetProperty("values", out var verdier))
            {
                foreach (var v in verdier.EnumerateArray())
                {
                    string? S(string navn) => v.TryGetProperty(navn, out var p) && p.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString()) ? p.GetString()!.Trim() : null;
                    var fullt = $"{S("firstName")} {S("lastName")}".Trim();
                    if (fullt.Length > 0 && v.TryGetProperty("id", out var id))
                    {
                        liste.Add(new TripletexKontakt(id.GetInt32(), fullt, S("email"), S("phoneNumberMobile") ?? S("phoneNumberWork")));
                    }
                }
            }
            return (liste, null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    // Oppretter (id null) eller oppdaterer en kontaktperson på kunden i Tripletex.
    public async Task<string?> LagreKontaktAsync(int? kontaktId, int kundeId, string navn, string? epost, string? telefon, CancellationToken ct = default)
    {
        try
        {
            var deler = navn.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fornavn = deler.Length > 1 ? string.Join(' ', deler[..^1]) : navn.Trim();
            var etternavn = deler.Length > 1 ? deler[^1] : "";
            var body = new Dictionary<string, object?>
            {
                ["firstName"] = fornavn,
                ["lastName"] = etternavn,
                ["email"] = GyldigEpost(epost),
                ["phoneNumberMobile"] = string.IsNullOrWhiteSpace(telefon) ? null : telefon.Trim(),
                ["customer"] = new { id = kundeId }
            };
            using var req = await LagAutorisertForespurselAsync(kontaktId is null ? HttpMethod.Post : HttpMethod.Put,
                kontaktId is null ? "contact" : $"contact/{kontaktId}", ct);
            req.Content = JsonContent.Create(body, options: JsonOpts);
            using var resp = await http.SendAsync(req, ct);
            return resp.IsSuccessStatusCode ? null : $"Tripletex svarte {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync(ct)}";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public sealed record KundeOppdatering(
        string Navn, string? OrganizationNumber, string? Email, string? Telefon,
        string? Adresse, string? Postnr, string? Sted);

    // Oppretter en ny kunde i Tripletex og returnerer den nye Tripletex-IDen +
    // det autogenererte kundenummeret. Kalles fra TripletexSyncService når en
    // portal-kunde ennå ikke har noe TripletexKundenummer.
    public async Task<(int? Id, string? CustomerNumber, string? Feilmelding)> OpprettKundeAsync(KundeOppdatering kunde, CancellationToken ct = default)
    {
        try
        {
            var body = LagKundeBody(kunde, ny: true);
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Post, "customer", ct);
            req.Content = JsonContent.Create(body, options: JsonOpts);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return (null, null, $"Tripletex svarte {(int)resp.StatusCode}: {raw}");
            }

            var parsed = JsonSerializer.Deserialize<ResponseWrapper<CustomerSvar>>(raw, JsonOpts);
            var opprettet = parsed?.Value ?? throw new InvalidOperationException("Tomt svar fra Tripletex ved oppretting av kunde.");
            return (opprettet.Id, opprettet.CustomerNumber, null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    // Oppdaterer en EKSISTERENDE kunde i Tripletex (identifisert av Tripletex sin
    // egen ID) med ferske verdier fra portalen.
    public async Task<string?> OppdaterKundeAsync(int tripletexId, KundeOppdatering kunde, CancellationToken ct = default)
    {
        try
        {
            var body = LagKundeBody(kunde);
            using var req = await LagAutorisertForespurselAsync(HttpMethod.Put, $"customer/{tripletexId}", ct);
            req.Content = JsonContent.Create(body, options: JsonOpts);
            using var resp = await http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                return null;
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            return $"Tripletex svarte {(int)resp.StatusCode}: {raw}";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    // Tripletex avviser hele kunden hvis e-posten ikke er gyldig. En ugyldig
    // adresse (skrivefeil, to adresser i ett felt) sendes derfor ikke med.
    public static string? GyldigEpost(string? epost)
    {
        var e = epost?.Trim();
        if (string.IsNullOrEmpty(e) || e.Contains(' ') || e.Contains(',') || e.Contains(';'))
        {
            return null;
        }
        return System.Text.RegularExpressions.Regex.IsMatch(e, @"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$") ? e : null;
    }

    // Ny kunde: sendemåten settes, ellers blir den EHF for bedrifter, og da krever
    // Tripletex postadresse (bedrifter uten adresse ble avvist). Med gyldig e-post
    // sendes fakturaen på e-post; uten e-post og adresse sendes den manuelt.
    // Ved oppdatering endres ikke sendemåten, så det firmaet har valgt i Tripletex, står.
    public static string? SendemateForNyKunde(KundeOppdatering kunde)
    {
        if (GyldigEpost(kunde.Email) is not null)
        {
            return "EMAIL";
        }
        var harAdresse = !string.IsNullOrWhiteSpace(kunde.Adresse) && !string.IsNullOrWhiteSpace(kunde.Postnr);
        return harAdresse ? null : "MANUAL";
    }

    private static object LagKundeBody(KundeOppdatering kunde, bool ny = false)
    {
        object? postadresse = string.IsNullOrWhiteSpace(kunde.Adresse) && string.IsNullOrWhiteSpace(kunde.Postnr) && string.IsNullOrWhiteSpace(kunde.Sted)
            ? null
            : new { addressLine1 = kunde.Adresse, postalCode = kunde.Postnr, city = kunde.Sted };

        var body = new Dictionary<string, object?>
        {
            ["name"] = kunde.Navn,
            ["organizationNumber"] = string.IsNullOrWhiteSpace(kunde.OrganizationNumber) ? null : kunde.OrganizationNumber,
            ["email"] = GyldigEpost(kunde.Email),
            // Fakturaene går til samme e-post.
            ["invoiceEmail"] = GyldigEpost(kunde.Email),
            ["phoneNumberMobile"] = kunde.Telefon,
            ["postalAddress"] = postadresse,
            // Uten org.nr. er kunden en privatperson.
            ["isPrivateIndividual"] = string.IsNullOrWhiteSpace(kunde.OrganizationNumber)
        };
        if (ny && SendemateForNyKunde(kunde) is { } sendemate)
        {
            body["invoiceSendMethod"] = sendemate;
        }
        return body;
    }

    // Ordrelinje: beskrivelse + antall + pris. Varene er fritekst (inntektskonto
    // og rapportering styres i portalen, se ArbeidsordreOkonomiBeregner). Linjer
    // med Varenummer (priskodene: arbeid, kjøring, tillegg) kobles til produktet
    // med samme nummer i Tripletex, så fakturaen får riktig vare og konto der.
    public sealed record OrdreLinjeInput(string Beskrivelse, decimal Antall, decimal EnhetsprisEksMva, string? Varenummer = null,
        decimal? Innpris = null, decimal RabattProsent = 0);

    // Oppretter en ordre i Tripletex "klar til fakturering" - IKKE det samme som
    // å fakturere den (det er et eget, separat :invoice-kall som denne
    // integrasjonen bevisst ikke gjør automatisk - fakturering skjer fortsatt
    // manuelt inne i Tripletex, akkurat som ved den gamle CSV-importen).
    //
    // To separate kall er nødvendig, bekreftet i praksis: å sende "orderLines"
    // direkte i POST /order-kroppen blir stille IGNORERT (ordren opprettes med
    // 0 linjer, ingen feilmelding) - linjene må opprettes separat mot
    // /order/orderline/list ETTER at selve ordren finnes. "deliveryDate" er i
    // tillegg påkrevd i praksis (422 "Kan ikke være null" uten den), selv om
    // Tripletex sin egen OpenAPI-spesifikasjon ikke lister den som påkrevd.
    public async Task<(int? Id, string? OrdreNummer, string? Feilmelding)> OpprettOrdreAsync(
        int tripletexKundeId, string? referanse, List<OrdreLinjeInput> linjer, CancellationToken ct = default)
    {
        try
        {
            if (linjer.Count == 0)
            {
                return (null, null, "Ingen ordrelinjer å sende - ordren har ingen varer eller montasjekost.");
            }

            var iDag = DateTime.Today.ToString("yyyy-MM-dd");
            var ordreBody = new
            {
                customer = new { id = tripletexKundeId },
                reference = referanse,
                orderDate = iDag,
                deliveryDate = iDag,
                // Linjene sendes med pris eks. mva. Står firmaet til å prioritere
                // priser inkl. mva, må ordren si fra, ellers avvises eller feiltolkes prisene.
                isPrioritizeAmountsIncludingVat = false
            };

            using var ordreReq = await LagAutorisertForespurselAsync(HttpMethod.Post, "order", ct);
            ordreReq.Content = JsonContent.Create(ordreBody, options: JsonOpts);
            using var ordreResp = await http.SendAsync(ordreReq, ct);
            var ordreRaw = await ordreResp.Content.ReadAsStringAsync(ct);
            if (!ordreResp.IsSuccessStatusCode)
            {
                return (null, null, $"Tripletex svarte {(int)ordreResp.StatusCode} ved oppretting av ordre: {ordreRaw}");
            }

            var ordreParsed = JsonSerializer.Deserialize<ResponseWrapper<OrderSvar>>(ordreRaw, JsonOpts);
            var opprettetOrdre = ordreParsed?.Value ?? throw new InvalidOperationException("Tomt svar fra Tripletex ved oppretting av ordre.");

            // Priskodene slås opp mot produktene i Tripletex. Finnes ikke
            // varenummeret der, sendes linjen som fritekst med koden foran.
            // Linjen får produktets egen mva-kode, så inntektskontoen på produktet brukes
            // (Tripletex bruker produktets konto bare når mva-koden er lik).
            var produkter = new Dictionary<string, TripletexProdukt>(StringComparer.OrdinalIgnoreCase);
            if (linjer.Any(l => !string.IsNullOrWhiteSpace(l.Varenummer)))
            {
                var (liste, _) = await HentProdukterAsync(ct);
                foreach (var p in liste)
                {
                    produkter.TryAdd(p.Nummer, p);
                }
            }
            var mvaId = await UtgaendeMvaIdAsync(ct);

            var linjerBody = linjer.Select(l =>
            {
                var linje = new Dictionary<string, object?>
                {
                    ["order"] = new { id = opprettetOrdre.Id },
                    ["description"] = l.Beskrivelse,
                    ["count"] = l.Antall,
                    ["unitPriceExcludingVatCurrency"] = l.EnhetsprisEksMva,
                    // Utgående mva, høy sats (25 %), slått opp i firmaets mva-koder.
                    ["vatType"] = new { id = mvaId }
                };
                // Innkjøpspris og rabatt, så linjen ser ut som når den legges inn i Tripletex.
                if (l.Innpris is { } inn && inn > 0)
                {
                    linje["unitCostCurrency"] = inn;
                }
                if (l.RabattProsent > 0)
                {
                    linje["discount"] = l.RabattProsent;
                }
                if (!string.IsNullOrWhiteSpace(l.Varenummer))
                {
                    if (produkter.TryGetValue(l.Varenummer.Trim(), out var produkt))
                    {
                        linje["product"] = new { id = produkt.Id };
                        if (produkt.MvaId is int produktMva)
                        {
                            linje["vatType"] = new { id = produktMva };
                        }
                    }
                    else
                    {
                        linje["description"] = $"{l.Varenummer} {l.Beskrivelse}";
                    }
                }
                return linje;
            }).ToList();

            using var linjerReq = await LagAutorisertForespurselAsync(HttpMethod.Post, "order/orderline/list", ct);
            linjerReq.Content = JsonContent.Create(linjerBody, options: JsonOpts);
            using var linjerResp = await http.SendAsync(linjerReq, ct);
            if (!linjerResp.IsSuccessStatusCode)
            {
                var linjerRaw = await linjerResp.Content.ReadAsStringAsync(ct);
                // Selve ordren ble opprettet selv om linjene feilet - gi
                // ordre-IDen tilbake likevel slik at den ikke bare "forsvinner",
                // men flagg tydelig at linjene mangler.
                return (opprettetOrdre.Id, opprettetOrdre.Number,
                    $"Ordre #{opprettetOrdre.Number} ble opprettet, men fikk ikke lagt til varelinjer ({(int)linjerResp.StatusCode}): {linjerRaw}");
            }

            return (opprettetOrdre.Id, opprettetOrdre.Number, null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    public sealed record TripletexFaktura(
        int Id, int? Nummer, DateTime? Dato, DateTime? Forfallsdato,
        int? KundeId, string? KundeNavn, decimal Belop, decimal Utestaende,
        bool ErKreditnota, string? OrdreNummer, int? OrdreId = null);

    // dateTo er «til, ikke med» (som invoiceDateTo i Tripletex): send dagen etter
    // siste dag som skal være med.
    // Henter fakturaer som faktisk er sendt/opprettet i Tripletex - i
    // motsetning til rapportene i /rapporter (som beregnes fra portalens
    // egne ferdigmeldte arbeidsordre), finnes denne dataen KUN i Tripletex
    // (selve faktureringen skjer fortsatt manuelt der), så her må vi hente
    // live. Brukes av Fakturaoversikt.razor og SalgPerKunde.razor.
    public async Task<(List<TripletexFaktura> Fakturaer, string? Feilmelding)> HentFakturaerAsync(
        DateTime dateFrom, DateTime dateTo, int? kundeId = null, CancellationToken ct = default)
    {
        // Sperren må komme før den delte cachen leses.
        if (!ErTilgjengeligForOrganisasjonen)
        {
            return ([], IkkeSattOpp);
        }

        var cacheNokkel = $"{tenantId}|{SesjonsNokkel}|{dateFrom:yyyy-MM-dd}|{dateTo:yyyy-MM-dd}|{kundeId}";
        if (_fakturaCache.TryGetValue(cacheNokkel, out var cachet) && DateTime.UtcNow - cachet.HentetUtc < FakturaCacheLevetid)
        {
            return (cachet.Fakturaer, cachet.Feilmelding);
        }

        var resultat = await HentFakturaerFraTripletexAsync(dateFrom, dateTo, kundeId, ct);

        // Cacher ikke feilresultater - en forbigående Tripletex-feil skal
        // ikke "henge igjen" og vises på nytt i flere minutter etter at
        // tjenesten er tilbake, den nye sidelastingen bør prøve på nytt.
        if (resultat.Feilmelding is null)
        {
            _fakturaCache[cacheNokkel] = (DateTime.UtcNow, resultat.Fakturaer, resultat.Feilmelding);
        }

        return resultat;
    }

    private async Task<(List<TripletexFaktura> Fakturaer, string? Feilmelding)> HentFakturaerFraTripletexAsync(
        DateTime dateFrom, DateTime dateTo, int? kundeId, CancellationToken ct)
    {
        try
        {
            // invoiceDateTo er «til, ikke med» i Tripletex: send dagen etter siste dag.
            var sti = $"invoice?invoiceDateFrom={dateFrom:yyyy-MM-dd}&invoiceDateTo={dateTo:yyyy-MM-dd}"
                + "&fields=id,invoiceNumber,invoiceDate,invoiceDueDate,customer(id,name),amount,amountOutstanding,isCreditNote,orders(id,number)";
            if (kundeId is not null)
            {
                sti += $"&customerId={kundeId}";
            }

            var (verdier, feil) = await HentAlleSiderAsync<InvoiceSvar>(sti, ct);
            if (feil is not null)
            {
                return ([], feil);
            }

            var fakturaer = verdier
                .Select(v => new TripletexFaktura(
                    v.Id, v.InvoiceNumber, v.InvoiceDate, v.InvoiceDueDate,
                    v.Customer?.Id, v.Customer?.Name, v.Amount ?? 0, v.AmountOutstanding ?? 0,
                    v.IsCreditNote ?? false, v.Orders?.FirstOrDefault()?.Number, v.Orders?.FirstOrDefault()?.Id))
                .ToList();
            return (fakturaer, null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    public sealed record TripletexProdukt(int Id, string Nummer, string Navn, decimal? Kostpris, decimal? Salgspris, int? MvaId = null);

    // Produktene i Tripletex (aktive, med varenummer). Brukes til å hente
    // priskodene, så kodene i portalen er de samme som varenumrene der.
    public async Task<(List<TripletexProdukt> Produkter, string? Feilmelding)> HentProdukterAsync(CancellationToken ct = default)
    {
        try
        {
            const string sti = "product?isInactive=false&fields=id,number,name,costExcludingVatCurrency,priceExcludingVatCurrency,vatType(id)";
            var (verdier, feil) = await HentAlleSiderAsync<ProductSvar>(sti, ct);
            if (feil is not null)
            {
                return ([], feil);
            }

            var produkter = verdier
                .Where(p => !string.IsNullOrWhiteSpace(p.Number))
                .Select(p => new TripletexProdukt(p.Id, p.Number!.Trim(), string.IsNullOrWhiteSpace(p.Name) ? p.Number!.Trim() : p.Name.Trim(),
                    p.CostExcludingVatCurrency, p.PriceExcludingVatCurrency, p.VatType?.Id))
                .OrderBy(p => p.Nummer)
                .ToList();
            return (produkter, null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    // --- Interne DTO-er for deserialisering (kun feltene vi faktisk bruker) ---

    private sealed class ProductSvar
    {
        public int Id { get; set; }
        public string? Number { get; set; }
        public string? Name { get; set; }
        public decimal? CostExcludingVatCurrency { get; set; }
        public decimal? PriceExcludingVatCurrency { get; set; }
        public IdSvar? VatType { get; set; }
    }

    private sealed class IdSvar
    {
        public int Id { get; set; }
    }

    private sealed class VatTypeSvar
    {
        public int Id { get; set; }

        [JsonConverter(typeof(SlakkStrengKonverterer))]
        public string? Number { get; set; }
        public decimal? Percentage { get; set; }
    }

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
        public long? CompanyId { get; set; }
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

        // Tripletex sin OpenAPI-spesifikasjon sier "string", men i praksis kan
        // f.eks. et autogenerert kundenummer komme tilbake som et rått
        // JSON-tall - System.Text.Json er strengt og kaster ellers en
        // JsonException midt i søket (bekreftet i praksis: "The JSON value
        // could not be converted to System.String" på nettopp dette feltet).
        [JsonConverter(typeof(SlakkStrengKonverterer))]
        public string? CustomerNumber { get; set; }

        [JsonConverter(typeof(SlakkStrengKonverterer))]
        public string? OrganizationNumber { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhoneNumberMobile { get; set; }
        public AddressSvar? PostalAddress { get; set; }
    }

    private sealed class AddressSvar
    {
        public string? AddressLine1 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
    }

    private sealed class OrderSvar
    {
        public int Id { get; set; }

        [JsonConverter(typeof(SlakkStrengKonverterer))]
        public string? Number { get; set; }
    }

    private sealed class InvoiceSvar
    {
        public int Id { get; set; }
        public int? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? InvoiceDueDate { get; set; }
        public InvoiceCustomerRefSvar? Customer { get; set; }
        public decimal? Amount { get; set; }
        public decimal? AmountOutstanding { get; set; }
        public bool? IsCreditNote { get; set; }
        public List<InvoiceOrderRefSvar>? Orders { get; set; }
    }

    private sealed class InvoiceCustomerRefSvar
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class InvoiceOrderRefSvar
    {
        public int Id { get; set; }

        [JsonConverter(typeof(SlakkStrengKonverterer))]
        public string? Number { get; set; }
    }

    // Godtar både JSON-streng og JSON-tall for et felt som .NET-siden
    // forventer som string - se kommentar på CustomerSvar over.
    private sealed class SlakkStrengKonverterer : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var heltall)
                    ? heltall.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Uventet JSON-type for strengfelt: {reader.TokenType}")
            };

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
