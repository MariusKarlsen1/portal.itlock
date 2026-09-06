using System.Net.Http.Headers;
using System.Text.Json;

namespace PortalItlock.Web.Services;

public class ResendReceivingClient(HttpClient http, IConfiguration config, ILogger<ResendReceivingClient> logger)
{
    public record VedleggData(string Filnavn, string ContentType, byte[] Data);
    public record FullInnhold(string? Tekst, List<VedleggData> Vedlegg);

    public async Task<FullInnhold?> HentFullInnholdAsync(string emailId)
    {
        var apiKey = config["Resend:FullAccessApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        foreach (var sti in new[] { $"emails/receiving/{emailId}", $"emails/{emailId}" })
        {
            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, sti);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                response = await http.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tekst = HentTekst(root, "text") ?? HentTekst(root, "html");
                var vedlegg = new List<VedleggData>();

                if (root.TryGetProperty("attachments", out var attArr) && attArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var att in attArr.EnumerateArray())
                    {
                        var filnavn = HentTekst(att, "filename") ?? "vedlegg";
                        var contentType = HentTekst(att, "content_type") ?? "application/octet-stream";
                        var innholdBase64 = HentTekst(att, "content");

                        if (innholdBase64 is null)
                        {
                            continue;
                        }

                        try
                        {
                            vedlegg.Add(new VedleggData(filnavn, contentType, Convert.FromBase64String(innholdBase64)));
                        }
                        catch (FormatException)
                        {
                        }
                    }
                }

                return new FullInnhold(tekst, vedlegg);
            }
            catch (JsonException)
            {
                continue;
            }
        }

        logger.LogWarning("Klarte ikke å hente fullt innhold for innkommende e-post {EmailId} fra Resend.", emailId);
        return null;
    }

    private static string? HentTekst(JsonElement element, string felt) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(felt, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
