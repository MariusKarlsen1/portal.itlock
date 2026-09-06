using System.Text.Json;

namespace PortalItlock.Web.Services;

public static class ResendInboundParser
{
    public record Resultat(string FraEpost, string? FraNavn, string Emne, string Innhold);

    public static Resultat Tolk(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : root;

            var fraEpost = HentTekst(data, "from", "email")
                ?? HentTekst(data, "from")
                ?? HentTekst(data, "from_email")
                ?? HentTekst(data, "sender")
                ?? "ukjent avsender";

            var fraNavn = HentTekst(data, "from", "name")
                ?? HentTekst(data, "from_name")
                ?? HentTekst(data, "sender_name");

            var emne = HentTekst(data, "subject") ?? "(uten emne)";

            var innhold = HentTekst(data, "text") ?? HentTekst(data, "html") ?? HentTekst(data, "body") ?? "";

            return new Resultat(fraEpost, fraNavn, emne, innhold);
        }
        catch (JsonException)
        {
            return new Resultat("ukjent avsender", null, "(uten emne)", "");
        }
    }

    private static string? HentTekst(JsonElement element, params string[] sti)
    {
        var gjeldende = element;
        foreach (var del in sti)
        {
            if (gjeldende.ValueKind != JsonValueKind.Object || !gjeldende.TryGetProperty(del, out var neste))
            {
                return null;
            }
            gjeldende = neste;
        }

        return gjeldende.ValueKind == JsonValueKind.String ? gjeldende.GetString() : null;
    }
}
