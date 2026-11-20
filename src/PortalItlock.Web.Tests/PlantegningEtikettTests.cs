using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

// Dørplantegning som PDF: etikettene (dørnummer og funksjoner) skal ikke ligge
// oppå hverandre, selv når dørene står tett.
public class PlantegningEtikettTests
{
    private static string Svg(List<Dor> dorer, int w = 2000, int h = 1400)
    {
        var plan = new Plantegning { Navn = "Test", Filnavn = "test.png", ContentType = "image/png", Data = [1, 2, 3] };
        var metode = typeof(PlantegningDorPdfService).GetMethod("BuildSvg", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)metode.Invoke(null, [plan, dorer, w, h])!;
    }

    private static List<Dor> TetteDorer()
    {
        var tilfeldig = new Random(4);
        var funk = new[] { "DL", "FG", "KO", "AK", "LL", "MOL", "PB", "RØ", "SS", "SSFG", "DA", "SY" };
        var dorer = new List<Dor>();
        var i = 0;
        // Tre klynger med mange dører tett, som på en skoletegning.
        foreach (var (kx, ky) in new[] { (30.0, 25.0), (45.0, 50.0), (60.0, 80.0) })
        {
            for (var n = 0; n < 18; n++)
            {
                var antall = tilfeldig.Next(1, 10);
                dorer.Add(new Dor
                {
                    Id = ++i,
                    Dornummer = $"B.01.{i:000}",
                    PosX = kx + tilfeldig.NextDouble() * 12,
                    PosY = ky + tilfeldig.NextDouble() * 12,
                    Funksjoner = funk.Take(antall).Select((f, j) => new DorFunksjon { Id = i * 100 + j, Forkortelse = f, Navn = f }).ToList()
                });
            }
        }
        return dorer;
    }

    [Fact]
    public void Etikettene_LiggerIkkeOppaHverandre()
    {
        var svg = Svg(TetteDorer());
        var ut = Environment.GetEnvironmentVariable("PLANTEGNING_SVG");
        if (!string.IsNullOrEmpty(ut))
        {
            File.WriteAllText(ut, svg);
        }

        // Etikettene er de hvite boksene (dørnummer); funksjonene ligger rett under.
        var rekt = Regex.Matches(svg, "<rect x='([-\\d.]+)' y='([-\\d.]+)' width='([\\d.]+)' height='([\\d.]+)' fill='white'")
            .Select(m => (X: D(m.Groups[1].Value), Y: D(m.Groups[2].Value), W: D(m.Groups[3].Value), H: D(m.Groups[4].Value)))
            .ToList();
        Assert.Equal(54, rekt.Count);
        var overlapp = 0;
        for (var a = 0; a < rekt.Count; a++)
        {
            for (var b = a + 1; b < rekt.Count; b++)
            {
                if (rekt[a].X < rekt[b].X + rekt[b].W && rekt[b].X < rekt[a].X + rekt[a].W
                    && rekt[a].Y < rekt[b].Y + rekt[b].H && rekt[b].Y < rekt[a].Y + rekt[a].H)
                {
                    overlapp++;
                }
            }
        }
        Assert.Equal(0, overlapp);
    }

    private static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);
}
