using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Sjekkpunktene per serviceavtale: standardpunkter kan skrus av, avtalen kan få
// egne punkter per beslagstype og for alle dører, og andre avtaler påvirkes ikke.
public class KontrollpunktPerAvtaleTests
{
    private static List<ServiceKontrollpunkt> Mal() =>
    [
        new() { Servicetype = "Dørlukker", Tekst = "Lukker helt fra 90°", Rekkefolge = 0 },
        new() { Servicetype = "Dørlukker", Tekst = "Fester sitter fast", Rekkefolge = 1 },
        new() { Servicetype = "Dørlukker", Tekst = "Lukker helt fra 90°", ProsjektId = 7, SkruddAv = true },
        new() { Servicetype = "Dørlukker", Tekst = "Sjekk dørstopper", ProsjektId = 7, Rekkefolge = 101 },
        new() { Servicetype = ServicerundeMotor.AlleDorer, Tekst = "Rydd rundt døren", ProsjektId = 7, Rekkefolge = 102 },
        new() { Servicetype = "Dørlukker", Tekst = "Annen avtale sitt punkt", ProsjektId = 8 },
        new() { Servicetype = "Dør", Tekst = "Varmekabel i terskel", DorId = 5 }
    ];

    [Fact]
    public void Avtalen_far_egne_punkter_og_slipper_de_som_er_skrudd_av()
    {
        var p = ServicerundeMotor.PunkterForDor(["Dørlukker"], Mal(), 5, 7).Select(x => x.Tekst).ToList();
        Assert.DoesNotContain("Lukker helt fra 90°", p);
        Assert.Contains("Fester sitter fast", p);
        Assert.Contains("Sjekk dørstopper", p);
        Assert.Contains("Rydd rundt døren", p);
        Assert.Contains("Varmekabel i terskel", p);
        Assert.DoesNotContain("Annen avtale sitt punkt", p);
    }

    [Fact]
    public void Uten_avtale_brukes_bare_standardpunktene()
    {
        var p = ServicerundeMotor.PunkterForDor(["Dørlukker"], Mal(), 1).Select(x => x.Tekst).ToList();
        Assert.Equal(["Lukker helt fra 90°", "Fester sitter fast"], p);
    }
}
