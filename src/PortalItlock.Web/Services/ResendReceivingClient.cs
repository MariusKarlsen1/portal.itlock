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

        JsonElement root;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"emails/receiving/{emailId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Klarte ikke å hente e-post {EmailId} fra Resend: {Status}", emailId, response.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            root = doc.RootElement.Clone();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "Feil under henting av e-post {EmailId} fra Resend.", emailId);
            return null;
        }

        var tekst = HentTekst(root, "text") ?? HentTekst(root, "html");
        var vedlegg = new List<VedleggData>();

        if (root.TryGetProperty("attachments", out var attArr) && attArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var att in attArr.EnumerateArray())
            {
                var attId = HentTekst(att, "id");
                if (attId is null)
                {
                    continue;
                }

                var filnavn = HentTekst(att, "filename") ?? "vedlegg";
                var contentType = HentTekst(att, "content_type") ?? "application/octet-stream";

                var data = await HentVedleggAsync(emailId, attId, apiKey);
                if (data is not null)
                {
                    vedlegg.Add(new VedleggData(filnavn, contentType, data));
                }
            }
        }

        return new FullInnhold(tekst, vedlegg);
    }

    private async Task<byte[]?> HentVedleggAsync(string emailId, string attachmentId, string apiKey)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"emails/receiving/{emailId}/attachments/{attachmentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var downloadUrl = HentTekst(doc.RootElement, "download_url");
            if (downloadUrl is null)
            {
                return null;
            }

            using var fileRequest = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            using var fileResponse = await http.SendAsync(fileRequest);
            if (!fileResponse.IsSuccessStatusCode)
            {
                return null;
            }

            return await fileResponse.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "Klarte ikke å laste ned vedlegg {AttachmentId} for e-post {EmailId}.", attachmentId, emailId);
            return null;
        }
    }

    private static string? HentTekst(JsonElement element, string felt) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(felt, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
