using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PortalItlock.Web.Services;

public class EmailService(HttpClient http, IConfiguration config)
{
    public async Task<bool> SendAsync(string til, string emne, string html, IEnumerable<(string Filnavn, byte[] Data)>? vedlegg = null)
    {
        var apiKey = config["Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(til))
        {
            return false;
        }

        var fraAdresse = config["Resend:FraAdresse"] ?? "onboarding@resend.dev";

        var payload = new Dictionary<string, object?>
        {
            ["from"] = fraAdresse,
            ["to"] = new[] { til },
            ["subject"] = emne,
            ["html"] = html
        };

        if (vedlegg is not null)
        {
            var vedleggListe = vedlegg
                .Select(v => new { filename = v.Filnavn, content = Convert.ToBase64String(v.Data) })
                .ToList();

            if (vedleggListe.Count > 0)
            {
                payload["attachments"] = vedleggListe;
            }
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}
