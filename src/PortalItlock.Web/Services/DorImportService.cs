using System.Text;
using System.Text.Json.Nodes;
using ClosedXML.Excel;

namespace PortalItlock.Web.Services;

public record DorImportRad(
    string Dornummer,
    string? Etasje,
    string? Sone,
    string? Romnr,
    string? Dortype,
    int? Bredde,
    int? Hoyde,
    string? Slagretning,
    string? Brann,
    string? Lyd,
    string? Energi,
    bool? FriBredde086,
    string? Notater);

public record DorImportResultat(List<DorImportRad> Rader, string? Feil);

// Tolker en opplastet dørskjema-fil (PDF, Excel, CSV eller bilde) fra en dørleverandør
// ved hjelp av Claude, og returnerer en strukturert liste med dører klar til
// forhåndsvisning før de opprettes/oppdateres i prosjektet (se DorImportModal.razor).
public class DorImportService(HttpClient http, IConfiguration config)
{
    public async Task<DorImportResultat> TolkFilAsync(byte[] data, string filnavn, CancellationToken ct = default)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new DorImportResultat([], "AI-tolkning er ikke satt opp enda - mangler API-nøkkel.");
        }

        JsonObject? kildeBlokk;
        try
        {
            kildeBlokk = Path.GetExtension(filnavn).ToLowerInvariant() switch
            {
                ".pdf" => new JsonObject
                {
                    ["type"] = "document",
                    ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = "application/pdf", ["data"] = Convert.ToBase64String(data) }
                },
                ".jpg" or ".jpeg" => BildeBlokk(data, "image/jpeg"),
                ".png" => BildeBlokk(data, "image/png"),
                ".xlsx" or ".xls" => new JsonObject { ["type"] = "text", ["text"] = LesRegnearkSomTekst(data) },
                ".csv" => new JsonObject { ["type"] = "text", ["text"] = Encoding.UTF8.GetString(data) },
                _ => null
            };
        }
        catch (Exception ex)
        {
            return new DorImportResultat([], $"Klarte ikke å lese filen: {ex.Message}");
        }

        if (kildeBlokk is null)
        {
            return new DorImportResultat([], "Filtypen støttes ikke. Bruk PDF, Excel (.xlsx), CSV eller bilde (.jpg/.png).");
        }

        var innhold = new JsonArray
        {
            kildeBlokk,
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = "Dette er et dørskjema/dørliste fra en dørleverandør eller arkitekt. Les ut ALLE dørene som er " +
                    "listet opp, og kall verktøyet lever_dorer med alle radene. Dørnummer (dør-ID/betegnelsen på døren) er " +
                    "obligatorisk for hver rad - bruk nøyaktig samme dørnummer som i kilden, det brukes til å matche mot " +
                    "eksisterende dører. La felt du ikke finner data for stå tomme/null, ikke gjett eller finn på verdier."
            }
        };

        var request = new JsonObject
        {
            ["model"] = "claude-sonnet-5",
            ["max_tokens"] = 8000,
            ["tools"] = new JsonArray { VerktoyDef() },
            ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = "lever_dorer" },
            ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = innhold } }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");

        string responseBody;
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(httpRequest, ct);
            responseBody = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            return new DorImportResultat([], $"Klarte ikke å kontakte AI-tjenesten: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            return new DorImportResultat([], $"AI-kallet feilet ({(int)response.StatusCode}). Prøv igjen, eller last opp en tydeligere fil.");
        }

        try
        {
            var svarJson = JsonNode.Parse(responseBody);
            var contentBlokker = svarJson?["content"]?.AsArray() ?? [];
            var verktoyBlokk = contentBlokker.FirstOrDefault(b =>
                b?["type"]?.GetValue<string>() == "tool_use" && b["name"]?.GetValue<string>() == "lever_dorer");

            var dorerNode = verktoyBlokk?["input"]?["dorer"]?.AsArray() ?? [];
            var rader = new List<DorImportRad>();
            foreach (var d in dorerNode)
            {
                var dornummer = Tekst(d, "dornummer");
                if (string.IsNullOrWhiteSpace(dornummer))
                {
                    continue;
                }

                rader.Add(new DorImportRad(
                    dornummer,
                    Tekst(d, "etasje"),
                    Tekst(d, "sone"),
                    Tekst(d, "romnr"),
                    Tekst(d, "dortype"),
                    Heltall(d, "bredde"),
                    Heltall(d, "hoyde"),
                    Tekst(d, "slagretning"),
                    Tekst(d, "brann"),
                    Tekst(d, "lyd"),
                    Tekst(d, "energi"),
                    Bool(d, "fribredde086"),
                    Tekst(d, "notater")));
            }

            return rader.Count == 0
                ? new DorImportResultat([], "Fant ingen dører i filen. Prøv en tydeligere fil, eller legg til dørene manuelt.")
                : new DorImportResultat(rader, null);
        }
        catch (Exception ex)
        {
            return new DorImportResultat([], $"Klarte ikke å tolke svaret fra AI-tjenesten: {ex.Message}");
        }
    }

    private static JsonObject BildeBlokk(byte[] data, string mediaType) => new()
    {
        ["type"] = "image",
        ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = mediaType, ["data"] = Convert.ToBase64String(data) }
    };

    private static string LesRegnearkSomTekst(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var wb = new XLWorkbook(stream);
        var sb = new StringBuilder();
        foreach (var ws in wb.Worksheets)
        {
            var brukt = ws.RangeUsed();
            if (brukt is null)
            {
                continue;
            }

            sb.AppendLine($"--- Ark: {ws.Name} ---");
            foreach (var rad in brukt.Rows())
            {
                sb.AppendLine(string.Join(" | ", rad.Cells().Select(c => c.GetString().Trim())));
            }
        }

        return sb.ToString();
    }

    private static string? Tekst(JsonNode? d, string felt)
    {
        var node = d?[felt];
        if (node is null)
        {
            return null;
        }

        try
        {
            var s = node.GetValue<string>();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
        catch
        {
            var s = node.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
    }

    private static int? Heltall(JsonNode? d, string felt)
    {
        var node = d?[felt];
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch
        {
            return int.TryParse(node.ToString(), out var i) ? i : null;
        }
    }

    private static bool? Bool(JsonNode? d, string felt)
    {
        var node = d?[felt];
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<bool>();
        }
        catch
        {
            return null;
        }
    }

    private static JsonObject VerktoyDef() => new()
    {
        ["name"] = "lever_dorer",
        ["description"] = "Levér den fullstendige listen med dører tolket fra dørskjemaet/filen.",
        ["input_schema"] = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["dorer"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["dornummer"] = new JsonObject { ["type"] = "string", ["description"] = "Dørnummer/dør-ID, obligatorisk" },
                            ["etasje"] = new JsonObject { ["type"] = "string" },
                            ["sone"] = new JsonObject { ["type"] = "string" },
                            ["romnr"] = new JsonObject { ["type"] = "string", ["description"] = "Romnummer og/eller romnavn" },
                            ["dortype"] = new JsonObject { ["type"] = "string" },
                            ["bredde"] = new JsonObject { ["type"] = "integer", ["description"] = "Bredde i mm" },
                            ["hoyde"] = new JsonObject { ["type"] = "integer", ["description"] = "Høyde i mm" },
                            ["slagretning"] = new JsonObject { ["type"] = "string" },
                            ["brann"] = new JsonObject { ["type"] = "string", ["description"] = "Brannklasse, f.eks. EI30, EI60" },
                            ["lyd"] = new JsonObject { ["type"] = "string", ["description"] = "Lydklasse" },
                            ["energi"] = new JsonObject { ["type"] = "string" },
                            ["fribredde086"] = new JsonObject { ["type"] = "boolean", ["description"] = "Om døren krever fri bredde 0,86m" },
                            ["notater"] = new JsonObject { ["type"] = "string", ["description"] = "Annen relevant info som ikke passer i de andre feltene" }
                        },
                        ["required"] = new JsonArray { "dornummer" }
                    }
                }
            },
            ["required"] = new JsonArray { "dorer" }
        }
    };
}
