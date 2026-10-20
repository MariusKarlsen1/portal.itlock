using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Leser ordrebekreftelser og pakksedler (PDF eller bilde) fra leverandører
// med Claude, og finner hvilken innkjøpsordre og hvilke ordrelinjer
// dokumentet gjelder. Bruker samme API-nøkkel som AI-assistenten
// (Anthropic:ApiKey). Selve matchingen mot ordrelinjene gjøres også her i
// koden (IO-nummer, leverandørens ordrenr, varenummer/EAN), så et svar fra
// modellen aldri blir brukt uten at det stemmer med en faktisk ordre.
//
// I utviklingsmiljøet uten nøkkel simuleres tolkingen ut fra ordren, slik
// at flyten kan prøves i demoen - det merkes tydelig i skjermbildet.
public partial class DokumentTolkService(HttpClient http, IConfiguration config, IHostEnvironment miljo)
{
    public const string Ordrebekreftelse = "ordrebekreftelse";
    public const string Pakkseddel = "pakkseddel";

    public sealed record TolketLinje(string? Varenummer, string? Beskrivelse, int? Antall, decimal? Enhetspris, DateTime? Leveringsdato, int? LinjeId);

    public sealed record TolketDokument(string Type, string? LeverandorNavn, string? LeverandorOrdrenr, string? VarOrdrenummer,
        string? Dokumentnummer, DateTime? Leveringsdato, List<TolketLinje> Linjer, bool Simulert = false, string? Feil = null);

    // Én dokumentlinje koblet til en ordrelinje (null = står ikke på ordren).
    public sealed record Kobling(TolketLinje Dokument, InnkjopsordreLinje? Linje);

    public static bool StottetFil(string? contentType) =>
        contentType is "application/pdf" or "image/jpeg" or "image/png" or "image/webp" or "image/gif";

    public bool KanTolke => !string.IsNullOrWhiteSpace(config["Anthropic:ApiKey"]) || miljo.IsDevelopment();

    public async Task<TolketDokument> TolkAsync(byte[] data, string contentType, string filnavn, string type,
        IReadOnlyList<Innkjopsordre> kandidater, IReadOnlyDictionary<int, Component> varer, CancellationToken ct = default)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return miljo.IsDevelopment()
                ? Simuler(filnavn, type, kandidater)
                : new TolketDokument(type, null, null, null, null, null, [], Feil: "Automatisk innlesing er ikke satt opp (mangler Anthropic:ApiKey). Fyll inn bekreftelsen for hånd.");
        }

        if (!StottetFil(contentType))
        {
            return new TolketDokument(type, null, null, null, null, null, [], Feil: "Bare PDF og bilder (JPG/PNG) kan leses inn.");
        }

        var dokumentBlokk = contentType == "application/pdf"
            ? new JsonObject { ["type"] = "document", ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = contentType, ["data"] = Convert.ToBase64String(data) } }
            : new JsonObject { ["type"] = "image", ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = contentType, ["data"] = Convert.ToBase64String(data) } };

        var request = new JsonObject
        {
            ["model"] = config["Anthropic:DokumentModel"] ?? "claude-sonnet-5-5",
            ["max_tokens"] = 4000,
            ["tools"] = new JsonArray { Verktoy() },
            ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = "registrer_dokument" },
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray { dokumentBlokk, new JsonObject { ["type"] = "text", ["text"] = Instruksjon(type, filnavn, kandidater, varer) } }
                }
            }
        };

        try
        {
            using var melding = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            melding.Headers.Add("x-api-key", apiKey);
            melding.Headers.Add("anthropic-version", "2023-06-01");
            melding.Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
            using var svar = await http.SendAsync(melding, ct);
            var tekst = await svar.Content.ReadAsStringAsync(ct);
            if (!svar.IsSuccessStatusCode)
            {
                return new TolketDokument(type, null, null, null, null, null, [], Feil: $"Innlesingen feilet ({(int)svar.StatusCode}). Prøv igjen, eller fyll inn for hånd.");
            }

            var input = JsonNode.Parse(tekst)?["content"]?.AsArray()
                .FirstOrDefault(b => b?["type"]?.GetValue<string>() == "tool_use")?["input"];
            return input is null
                ? new TolketDokument(type, null, null, null, null, null, [], Feil: "Fant ingen varelinjer i dokumentet.")
                : FraJson(type, input);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new TolketDokument(type, null, null, null, null, null, [], Feil: "Fikk ikke kontakt med tjenesten som leser dokumentet. Prøv igjen.");
        }
    }

    private static string Instruksjon(string type, string filnavn, IReadOnlyList<Innkjopsordre> kandidater, IReadOnlyDictionary<int, Component> varer)
    {
        var sb = new StringBuilder();
        sb.AppendLine(type == Pakkseddel
            ? "Dokumentet er en pakkseddel/følgeseddel fra en leverandør. Les ut hva som faktisk er levert i denne sendingen."
            : "Dokumentet er en ordrebekreftelse fra en leverandør. Les ut hva leverandøren bekrefter: antall, netto enhetspris (etter rabatt, eks. mva) og leveringsdato per linje.");
        sb.AppendLine("Ta med alle varelinjer, men ikke frakt, gebyrer eller miljøavgift som egne linjer. Restnoterte linjer tas med med antallet som faktisk bekreftes/leveres (0 hvis ingenting).");
        sb.AppendLine("Vår ordre (innkjøpsordre) har nummer på formen IO-1234 - finn den hvis den står i dokumentet (f.eks. som «Deres ref», «Deres ordrenr» eller «Bestillingsnr»).");
        sb.AppendLine($"Filnavn: {filnavn}");
        sb.AppendLine("Åpne innkjøpsordre hos oss som dokumentet trolig gjelder (koble hver dokumentlinje til linje_id når du er sikker):");
        foreach (var o in kandidater.Take(40))
        {
            sb.AppendLine($"{o.Visningsnummer} - {o.LeverandorNavn}{(o.LeverandorOrdrenr is null ? "" : $" (leverandørens ordrenr {o.LeverandorOrdrenr})")}:");
            foreach (var l in o.Linjer)
            {
                var c = varer.GetValueOrDefault(l.ComponentId);
                sb.AppendLine($"  linje_id {l.Id}: varenr {l.Varenummer ?? "-"}{(string.IsNullOrWhiteSpace(c?.Gtin) ? "" : $", EAN {c.Gtin}")}, {l.Navn}, bestilt {l.Antall}, pris {l.Pris.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        sb.AppendLine("Datoer skrives som YYYY-MM-DD. Tall med punktum som desimaltegn.");
        return sb.ToString();
    }

    private static JsonObject Verktoy() => new()
    {
        ["name"] = "registrer_dokument",
        ["description"] = "Registrerer innholdet i ordrebekreftelsen eller pakkseddelen.",
        ["input_schema"] = JsonNode.Parse("""
        {
          "type": "object",
          "properties": {
            "io_nummer": { "type": ["string", "null"], "description": "Vårt innkjøpsordrenummer, f.eks. IO-1004, hvis det står i dokumentet" },
            "leverandor": { "type": ["string", "null"] },
            "leverandor_ordrenr": { "type": ["string", "null"], "description": "Leverandørens eget ordrenummer" },
            "dokumentnummer": { "type": ["string", "null"], "description": "Pakkseddel-/følgeseddelnummer eller bekreftelsesnummer" },
            "leveringsdato": { "type": ["string", "null"], "description": "Felles leveringsdato for hele ordren, YYYY-MM-DD" },
            "linjer": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "linje_id": { "type": ["integer", "null"], "description": "linje_id fra listen over våre ordrelinjer, hvis sikker" },
                  "varenummer": { "type": ["string", "null"] },
                  "beskrivelse": { "type": ["string", "null"] },
                  "antall": { "type": ["integer", "null"] },
                  "enhetspris": { "type": ["number", "null"], "description": "Netto pris per enhet etter rabatt, eks. mva" },
                  "leveringsdato": { "type": ["string", "null"], "description": "YYYY-MM-DD" }
                },
                "required": ["antall"]
              }
            }
          },
          "required": ["linjer"]
        }
        """)
    };

    private static TolketDokument FraJson(string type, JsonNode input)
    {
        static string? S(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;
        static DateTime? D(JsonNode? n) => S(n) is { } s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.Date : null;
        static int? I(JsonNode? n) => n is JsonValue v && (v.TryGetValue<int>(out var i) ? i : v.TryGetValue<double>(out var d) ? (int)Math.Round(d) : (int?)null) is { } x ? x : null;
        static decimal? M(JsonNode? n) => n is JsonValue v && v.TryGetValue<double>(out var d) ? Math.Round((decimal)d, 2) : null;

        var linjer = (input["linjer"]?.AsArray() ?? [])
            .Where(l => l is not null)
            .Select(l => new TolketLinje(S(l!["varenummer"]), S(l["beskrivelse"]), I(l["antall"]), M(l["enhetspris"]), D(l["leveringsdato"]), I(l["linje_id"])))
            .ToList();
        return new TolketDokument(type, S(input["leverandor"]), S(input["leverandor_ordrenr"]), S(input["io_nummer"]),
            S(input["dokumentnummer"]), D(input["leveringsdato"]), linjer);
    }

    // ---------- Simulering (bare utviklingsmiljø uten nøkkel) ----------

    private static TolketDokument Simuler(string filnavn, string type, IReadOnlyList<Innkjopsordre> kandidater)
    {
        var ordre = FinnFraNummer(filnavn, kandidater) ?? kandidater.FirstOrDefault();
        if (ordre is null)
        {
            return new TolketDokument(type, null, null, null, null, null, [], Simulert: true, Feil: "Ingen åpne bestillinger å lese inn mot.");
        }

        var dato = (ordre.OnsketLevering ?? DateTime.Today.AddDays(5)).Date;
        var linjer = ordre.Linjer.OrderBy(l => l.Id)
            .Select(l => type == Pakkseddel
                ? new TolketLinje(l.Varenummer, l.Navn, l.Rest, null, null, null)
                : new TolketLinje(l.Varenummer, l.Navn, l.Antall, l.Pris, dato, null))
            .ToList();
        return new TolketDokument(type, ordre.LeverandorNavn, ordre.LeverandorOrdrenr ?? $"SO-{ordre.Nummer + 40000}", ordre.Visningsnummer,
            type == Pakkseddel ? $"PS-{ordre.Nummer + 70000}" : null, dato, linjer, Simulert: true);
    }

    // ---------- Matching mot ordre og linjer ----------

    [GeneratedRegex(@"IO[\s\-_]?(\d{4,6})", RegexOptions.IgnoreCase)]
    private static partial Regex IoNummer();

    private static Innkjopsordre? FinnFraNummer(string? tekst, IReadOnlyList<Innkjopsordre> kandidater) =>
        tekst is not null && IoNummer().Match(tekst) is { Success: true } m && int.TryParse(m.Groups[1].Value, out var nr)
            ? kandidater.FirstOrDefault(o => o.Nummer == nr)
            : null;

    private static string Norm(string? s) => string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    // Hvilken ordre dokumentet gjelder: IO-nummeret i dokumentet eller
    // filnavnet, ellers leverandørens ordrenr, ellers ordren der flest
    // varenumre stemmer (og leverandørnavnet ligner).
    public static Innkjopsordre? FinnOrdre(TolketDokument d, string filnavn, IReadOnlyList<Innkjopsordre> kandidater, IReadOnlyDictionary<int, Component> varer)
    {
        if ((FinnFraNummer(d.VarOrdrenummer, kandidater) ?? FinnFraNummer(filnavn, kandidater)) is { } direkte)
        {
            return direkte;
        }

        if (d.LeverandorOrdrenr is { } lo && kandidater.FirstOrDefault(o => Norm(o.LeverandorOrdrenr) == Norm(lo) && Norm(lo) != "") is { } viaLev)
        {
            return viaLev;
        }

        return kandidater
            .Select(o => (Ordre: o, Poeng: Koble(o, d, varer).Count(k => k.Linje is not null) * 2
                + (d.LeverandorNavn is { } ln && (Norm(o.LeverandorNavn).Contains(Norm(ln)) || Norm(ln).Contains(Norm(o.LeverandorNavn))) ? 1 : 0)))
            .Where(x => x.Poeng >= 2)
            .OrderByDescending(x => x.Poeng).ThenByDescending(x => x.Ordre.Nummer)
            .Select(x => x.Ordre).FirstOrDefault();
    }

    // Kobler dokumentlinjene til ordrelinjene: linje_id fra modellen (hvis
    // den finnes på ordren), ellers varenummer, EAN eller varenavn.
    public static List<Kobling> Koble(Innkjopsordre ordre, TolketDokument d, IReadOnlyDictionary<int, Component> varer)
    {
        var brukt = new HashSet<int>();
        var resultat = new List<Kobling>();
        foreach (var dl in d.Linjer)
        {
            var linje = ordre.Linjer.FirstOrDefault(l => l.Id == dl.LinjeId && !brukt.Contains(l.Id))
                ?? ordre.Linjer.FirstOrDefault(l => !brukt.Contains(l.Id) && Norm(dl.Varenummer) != "" && (
                    Norm(l.Varenummer) == Norm(dl.Varenummer)
                    || Norm(varer.GetValueOrDefault(l.ComponentId)?.Gtin) == Norm(dl.Varenummer)
                    || Norm(varer.GetValueOrDefault(l.ComponentId)?.Produktkode) == Norm(dl.Varenummer)))
                ?? ordre.Linjer.FirstOrDefault(l => !brukt.Contains(l.Id) && Norm(dl.Beskrivelse) != "" && Norm(l.Navn) == Norm(dl.Beskrivelse));
            if (linje is not null)
            {
                brukt.Add(linje.Id);
            }

            resultat.Add(new Kobling(dl, linje));
        }

        return resultat;
    }
}
