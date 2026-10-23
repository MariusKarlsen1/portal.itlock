using System.Text.Json;

namespace PortalItlock.Web.Services;

public static class ResendInboundParser
{
    public record Resultat(string FraEpost, string? FraNavn, string Emne, string Innhold, string? EmailId);

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

            var emailId = HentTekst(data, "email_id") ?? HentTekst(data, "id");

            return new Resultat(fraEpost, fraNavn, emne, innhold, emailId);
        }
        catch (JsonException)
        {
            return new Resultat("ukjent avsender", null, "(uten emne)", "", null);
        }
    }

    // Alle mottakeradresser (to, cc, bcc og received_for), som rene adresser med små bokstaver.
    // Brukes til å finne hvilken organisasjon e-posten skal til.
    public static List<string> HentMottakere(string rawJson)
    {
        var liste = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : root;
            // received_for er den faktiske mottakeren, også når adressen sto som
            // kopi eller blindkopi.
            foreach (var felt in new[] { "to", "cc", "bcc", "received_for" })
            {
                if (!data.TryGetProperty(felt, out var verdi))
                {
                    continue;
                }

                var elementer = verdi.ValueKind == JsonValueKind.Array ? verdi.EnumerateArray().ToList() : [verdi];
                foreach (var e in elementer)
                {
                    var tekst = e.ValueKind == JsonValueKind.String ? e.GetString()
                        : e.ValueKind == JsonValueKind.Object && e.TryGetProperty("email", out var ep) ? ep.GetString() : null;
                    if (string.IsNullOrWhiteSpace(tekst))
                    {
                        continue;
                    }

                    var start = tekst.LastIndexOf('<');
                    var slutt = tekst.LastIndexOf('>');
                    var adresse = start >= 0 && slutt > start ? tekst[(start + 1)..slutt] : tekst;
                    liste.Add(adresse.Trim().ToLowerInvariant());
                }
            }
        }
        catch (JsonException)
        {
        }

        return liste;
    }

    // HTML-delen fra selve webhooken (brukes hvis den ikke kan hentes fra Resend-API-et).
    public static string? HentHtml(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : root;
            return HentTekst(data, "html");
        }
        catch (JsonException)
        {
            return null;
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
