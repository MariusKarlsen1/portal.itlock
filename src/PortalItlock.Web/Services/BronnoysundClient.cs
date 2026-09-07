using System.Text.Json;

namespace PortalItlock.Web.Services;

public class BronnoysundClient(HttpClient http)
{
    public record Resultat(string Navn, string? Adresse, string? Postnr, string? Sted);

    public async Task<Resultat?> SlaOppAsync(string orgnr)
    {
        var renset = new string(orgnr.Where(char.IsDigit).ToArray());
        if (renset.Length != 9)
        {
            return null;
        }

        try
        {
            using var response = await http.GetAsync($"enheter/{renset}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            var navn = HentTekst(root, "navn");
            if (string.IsNullOrWhiteSpace(navn))
            {
                return null;
            }

            string? adresse = null;
            string? postnr = null;
            string? sted = null;

            if (root.TryGetProperty("forretningsadresse", out var adr) && adr.ValueKind == JsonValueKind.Object)
            {
                if (adr.TryGetProperty("adresse", out var linjer) && linjer.ValueKind == JsonValueKind.Array)
                {
                    adresse = string.Join(", ", linjer.EnumerateArray()
                        .Select(l => l.GetString())
                        .Where(l => !string.IsNullOrWhiteSpace(l)));
                }

                postnr = HentTekst(adr, "postnummer");
                sted = HentTekst(adr, "poststed");
            }

            return new Resultat(navn, string.IsNullOrWhiteSpace(adresse) ? null : adresse, postnr, sted);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            return null;
        }
    }

    private static string? HentTekst(JsonElement element, string felt) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(felt, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
