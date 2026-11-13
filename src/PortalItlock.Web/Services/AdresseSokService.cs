using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortalItlock.Web.Services;

public sealed record AdresseTreff(string Adresse, string Postnr, string Sted)
{
    public string Visning => $"{Adresse}, {Postnr} {Sted}";
}

/// <summary>
/// Adresseforslag fra Kartverkets åpne adresse-API (ws.geonorge.no/adresser/v1).
/// Krever ingen nøkkel. Feiler oppslaget, kommer det bare ingen forslag.
/// </summary>
public class AdresseSokService(HttpClient http)
{
    public async Task<List<AdresseTreff>> SokAsync(string? tekst, CancellationToken ct = default)
    {
        var sok = (tekst ?? "").Trim();
        if (sok.Length < 3)
        {
            return [];
        }

        try
        {
            var url = $"adresser/v1/sok?sok={Uri.EscapeDataString(sok)}&treffPerSide=8&fuzzy=true&filtrer=adresser.adressetekst,adresser.postnummer,adresser.poststed";
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                return [];
            }
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var svar = await JsonSerializer.DeserializeAsync<Svar>(stream, cancellationToken: ct);
            return (svar?.Adresser ?? [])
                .Where(a => !string.IsNullOrWhiteSpace(a.Adressetekst) && !string.IsNullOrWhiteSpace(a.Postnummer))
                .Select(a => new AdresseTreff(a.Adressetekst!, a.Postnummer!, a.Poststed ?? ""))
                .DistinctBy(a => a.Visning)
                .ToList();
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException or JsonException or OperationCanceledException)
        {
            return [];
        }
    }

    private sealed class Svar
    {
        [JsonPropertyName("adresser")] public List<Adr>? Adresser { get; set; }
    }

    private sealed class Adr
    {
        [JsonPropertyName("adressetekst")] public string? Adressetekst { get; set; }
        [JsonPropertyName("postnummer")] public string? Postnummer { get; set; }
        [JsonPropertyName("poststed")] public string? Poststed { get; set; }
    }
}
