using System.Net.Http.Headers;
using System.Text.Json;

namespace PortalItlock.Web.Services;

public class GeocodingService(HttpClient http)
{
    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string adresse, string? postnr, string? sted)
    {
        var deler = new[] { adresse, postnr, sted }.Where(d => !string.IsNullOrWhiteSpace(d));
        var sok = string.Join(", ", deler);
        if (sok.Length == 0)
        {
            return null;
        }

        var url = $"search?q={Uri.EscapeDataString(sok)}&format=json&limit=1&countrycodes=no";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PortalItlockFullKontroll", "1.0"));

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            // Nettverket kan være utilgjengelig eller for tregt - ikke la kartoppslag blokkere siden.
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var treff = await JsonSerializer.DeserializeAsync<List<NominatimTreff>>(stream);
        var forste = treff?.FirstOrDefault();
        if (forste is null)
        {
            return null;
        }

        if (!double.TryParse(forste.lat, System.Globalization.CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(forste.lon, System.Globalization.CultureInfo.InvariantCulture, out var lon))
        {
            return null;
        }

        return (lat, lon);
    }

    private sealed class NominatimTreff
    {
        public string lat { get; set; } = "";
        public string lon { get; set; } = "";
    }
}
