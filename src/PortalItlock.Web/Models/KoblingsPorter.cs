using System.Globalization;
using System.Text.Json;

namespace PortalItlock.Web.Models;

// Egne tilkoblingspunkter på et koblingsskjema-symbol (KoblingsSymbol.PorterJson):
// [[x,y],...] der x og y er andel (0-1) av symbolets bredde og høyde.
public static class KoblingsPorter
{
    public const int Maks = 24;

    public static List<(double X, double Y)> Les(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }
        try
        {
            return (JsonSerializer.Deserialize<double[][]>(json) ?? [])
                .Where(p => p is { Length: 2 } && double.IsFinite(p[0]) && double.IsFinite(p[1]))
                .Select(p => (Math.Clamp(p[0], 0, 1), Math.Clamp(p[1], 0, 1)))
                .Take(Maks)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Skriv(List<(double X, double Y)> porter) =>
        porter.Count == 0
            ? null
            : "[" + string.Join(",", porter.Select(p =>
                $"[{Math.Round(Math.Clamp(p.X, 0, 1), 4).ToString(CultureInfo.InvariantCulture)},{Math.Round(Math.Clamp(p.Y, 0, 1), 4).ToString(CultureInfo.InvariantCulture)}]")) + "]";
}
