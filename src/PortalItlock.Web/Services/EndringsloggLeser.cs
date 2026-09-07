using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortalItlock.Web.Services;

public static class EndringsloggLeser
{
    public record Innslag(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("dato")] DateTimeOffset Dato,
        [property: JsonPropertyName("tittel")] string Tittel,
        [property: JsonPropertyName("innhold")] string Innhold);

    public static List<Innslag> LesAlle(string filsti)
    {
        if (!File.Exists(filsti))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(filsti);
            return JsonSerializer.Deserialize<List<Innslag>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
