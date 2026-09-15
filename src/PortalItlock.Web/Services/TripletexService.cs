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

            // MERK: ingen ledende skråstrek - HttpClient sin BaseAddress har
            // en sti (".../v2/"), og en relativ URI som starter med "/" blir
            // tolket som absolutt fra host-roten, som DROPPER "/v2"-delen av
            // BaseAddress helt (klassisk HttpClient-fallgruve). Uten dette
            // traff kallet https://api-test.tripletex.tech/token/... (uten
            // /v2), som ikke finnes - Tripletex sin vanlige nettside svarte
            // da med en full HTML-404-side i stedet for en JSON-API-feil.
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
                "token/session/>whoAmI?fields=employee(firstName,lastName),company(name)", ct);
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
            var sti = "customer?count=1000&fields=id,name,customerNumber,organizationNumber,email,phoneNumber,phoneNumberMobile,postalAddress(addressLine1,postalCode,city)";
            if (endretSiden is not null)
            {
                sti += $"&changedSince={Uri.EscapeDataString(endretSiden.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"))}";
            }

            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, sti, ct);
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

    private static TripletexKunde TilTripletexKunde(CustomerSvar k) => new(
        k.Id, k.Name ?? "(uten navn)", k.CustomerNumber, k.OrganizationNumber, k.Email,
        k.PhoneNumberMobile ?? k.PhoneNumber,
        k.PostalAddress?.AddressLine1, k.PostalAddress?.PostalCode, k.PostalAddress?.City);

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
            var body = LagKundeBody(kunde);
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

    private static object LagKundeBody(KundeOppdatering kunde)
    {
        object? postadresse = string.IsNullOrWhiteSpace(kunde.Adresse) && string.IsNullOrWhiteSpace(kunde.Postnr) && string.IsNullOrWhiteSpace(kunde.Sted)
            ? null
            : new { addressLine1 = kunde.Adresse, postalCode = kunde.Postnr, city = kunde.Sted };

        return new
        {
            name = kunde.Navn,
            organizationNumber = string.IsNullOrWhiteSpace(kunde.OrganizationNumber) ? null : kunde.OrganizationNumber,
            email = kunde.Email,
            phoneNumberMobile = kunde.Telefon,
            postalAddress = postadresse,
            isCustomer = true
        };
    }

    // Ren fritekstlinje (beskrivelse + antall + pris) - portalen kobler
    // bevisst IKKE varer mot Tripletex-produkter; inntektskonto/rapportering
    // er portalens eget ansvar (se ArbeidsordreOkonomiBeregner), ordren i
    // Tripletex er kun "klar til fakturering"-grunnlaget.
    public sealed record OrdreLinjeInput(string Beskrivelse, decimal Antall, decimal EnhetsprisEksMva);

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
                deliveryDate = iDag
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

            var linjerBody = linjer.Select(l => new
            {
                order = new { id = opprettetOrdre.Id },
                description = l.Beskrivelse,
                count = l.Antall,
                unitPriceExcludingVatCurrency = l.EnhetsprisEksMva,
                // itlock bruker 25% mva på alt (samme kode som den eksisterende
                // TripletexOrdreCsvService sitt CSV-importformat bruker) - se
                // MVA-kodeoversikten i Tripletex hvis dette ikke stemmer.
                vatType = new { id = 3 }
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
        bool ErKreditnota, string? OrdreNummer);

    // Henter fakturaer som faktisk er sendt/opprettet i Tripletex - i
    // motsetning til rapportene i /rapporter (som beregnes fra portalens
    // egne ferdigmeldte arbeidsordre), finnes denne dataen KUN i Tripletex
    // (selve faktureringen skjer fortsatt manuelt der), så her må vi hente
    // live. Brukes av Fakturaoversikt.razor og SalgPerKunde.razor.
    public async Task<(List<TripletexFaktura> Fakturaer, string? Feilmelding)> HentFakturaerAsync(
        DateTime dateFrom, DateTime dateTo, int? kundeId = null, CancellationToken ct = default)
    {
        try
        {
            var sti = $"invoice?count=1000&invoiceDateFrom={dateFrom:yyyy-MM-dd}&invoiceDateTo={dateTo:yyyy-MM-dd}"
                + "&fields=id,invoiceNumber,invoiceDate,invoiceDueDate,customer(id,name),amount,amountOutstanding,isCreditNote,orders(number)";
            if (kundeId is not null)
            {
                sti += $"&customerId={kundeId}";
            }

            using var req = await LagAutorisertForespurselAsync(HttpMethod.Get, sti, ct);
            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                return ([], $"Tripletex svarte {(int)resp.StatusCode}: {raw}");
            }

            var parsed = JsonSerializer.Deserialize<ListResponse<InvoiceSvar>>(raw, JsonOpts);
            var fakturaer = (parsed?.Values ?? [])
                .Select(v => new TripletexFaktura(
                    v.Id, v.InvoiceNumber, v.InvoiceDate, v.InvoiceDueDate,
                    v.Customer?.Id, v.Customer?.Name, v.Amount ?? 0, v.AmountOutstanding ?? 0,
                    v.IsCreditNote ?? false, v.Orders?.FirstOrDefault()?.Number))
                .ToList();
            return (fakturaer, null);
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
