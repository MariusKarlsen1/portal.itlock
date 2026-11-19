using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Fri tekst ved enden av hver leder i en flerlederkabel.
public class KoblingsLederTekstTests
{
    private static KoblingsStrek Kabel(List<KoblingsLeder> ledere) => new()
    {
        PunkterJson = "[{\"X\":50,\"Y\":10},{\"X\":50,\"Y\":60}]",
        LedereJson = KoblingsKabel.Skriv(ledere),
    };

    [Fact]
    public void TekstIBeggeEnder_PaRiktigLeder()
    {
        var ledere = KoblingsKabel.MedAntall([], 3);
        ledere[0].TekstStart = "+12V";
        ledere[0].TekstSlutt = "V+";
        ledere[2].TekstSlutt = "NO";
        ledere[1].Farge = "Hvit";
        ledere[1].TekstStart = "D0";
        var s = Kabel(ledere);
        Assert.True(KoblingsStrekTekst.HarLederTekst(s));

        var v = KoblingsKabel.LagVifte(System.Text.Json.JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(s.PunkterJson)!, KoblingsKabel.Les(s.LedereJson), 1);
        var t = KoblingsStrekTekst.ForLedere(s, v, 1).ToList();
        Assert.Equal(["+12V", "D0", "V+", "NO"], t.Select(x => x.P.Tekst).OrderBy(x => x == "+12V" ? 0 : x == "D0" ? 1 : x == "V+" ? 2 : 3));
        Assert.All(t.Where(x => x.P.Tekst is "+12V" or "D0"), x => Assert.True(x.P.Y < 10));
        Assert.All(t.Where(x => x.P.Tekst is "V+" or "NO"), x => Assert.True(x.P.Y > 60));
        Assert.Equal("#14213D", t.Single(x => x.P.Tekst == "D0").Farge);
    }

    [Fact]
    public void UtenTekst_IngenEtiketter() =>
        Assert.False(KoblingsStrekTekst.HarLederTekst(Kabel(KoblingsKabel.MedAntall([], 4))));
}
