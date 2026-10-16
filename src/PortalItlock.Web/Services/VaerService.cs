using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace PortalItlock.Web.Services;

// Været på forsiden: temperatur og vær nå for stedet organisasjonen holder til
// (Tenant.Sted), fra Meteorologisk institutt (api.met.no - samme kilde som yr.no).
// Stedet slås opp til koordinater via GeocodingService. Alt mellomlagres, så
// MET bare spørres én gang i halvtimen per sted, og forsiden aldri feiler
// hvis været ikke kan hentes (da vises bare dato og sted).
public class VaerService(HttpClient http, GeocodingService geokoding, IMemoryCache cache)
{
    public sealed record Vaer(string Sted, int Temperatur, string Beskrivelse, string Ikon);

    public async Task<Vaer?> HentAsync(string sted)
    {
        if (string.IsNullOrWhiteSpace(sted))
        {
            return null;
        }

        var nokkel = "vaer:" + sted.Trim().ToLowerInvariant();
        if (cache.TryGetValue(nokkel, out Vaer? lagret))
        {
            return lagret;
        }

        try
        {
            var koordinater = await cache.GetOrCreateAsync("vaer-sted:" + sted.Trim().ToLowerInvariant(), async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);
                return await geokoding.GeocodeAsync("", null, sted);
            });

            if (koordinater is not { } k)
            {
                return null;
            }

            var url = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"weatherapi/locationforecast/2.0/compact?lat={k.Latitude:0.####}&lon={k.Longitude:0.####}");
            using var svar = await http.GetAsync(url);
            if (!svar.IsSuccessStatusCode)
            {
                return null;
            }

            using var dok = await JsonDocument.ParseAsync(await svar.Content.ReadAsStreamAsync());
            var forst = dok.RootElement.GetProperty("properties").GetProperty("timeseries")[0].GetProperty("data");
            var temp = forst.GetProperty("instant").GetProperty("details").GetProperty("air_temperature").GetDouble();
            var symbol = forst.TryGetProperty("next_1_hours", out var neste)
                ? neste.GetProperty("summary").GetProperty("symbol_code").GetString() ?? ""
                : "";

            var (beskrivelse, ikon) = Oversett(symbol);
            var vaer = new Vaer(sted.Trim(), (int)Math.Round(temp), beskrivelse, ikon);
            cache.Set(nokkel, vaer, TimeSpan.FromMinutes(30));
            return vaer;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // MET sine symbolkoder (f.eks. "partlycloudy_day") til norsk tekst og ett av
    // ikonene forsiden tegner: sol, solsky, sky, regn, sno, torden.
    private static (string Beskrivelse, string Ikon) Oversett(string symbol)
    {
        var s = symbol.Split('_')[0];
        return s switch
        {
            "clearsky" => ("Klarvær", "sol"),
            "fair" => ("Lettskyet", "solsky"),
            "partlycloudy" => ("Delvis skyet", "solsky"),
            "cloudy" => ("Skyet", "sky"),
            "fog" => ("Tåke", "sky"),
            _ when s.Contains("thunder") => ("Torden", "torden"),
            _ when s.Contains("snow") => (s.Contains("showers") ? "Snøbyger" : "Snø", "sno"),
            _ when s.Contains("sleet") => ("Sludd", "sno"),
            "lightrain" => ("Lett regn", "regn"),
            "heavyrain" => ("Kraftig regn", "regn"),
            _ when s.Contains("rainshowers") => ("Regnbyger", "regn"),
            _ when s.Contains("rain") => ("Regn", "regn"),
            _ => ("", "solsky")
        };
    }
}
