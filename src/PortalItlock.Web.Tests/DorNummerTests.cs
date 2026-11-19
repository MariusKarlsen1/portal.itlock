using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Dørnummer er unikt i prosjektet, og dørfunksjoner står alltid i samme rekkefølge.
public class DorNummerTests
{
    [Fact]
    public void Ledig_GirNesteFrieNavn()
    {
        Assert.Equal("Ny dør", DorNummer.Ledig(["D1"], "Ny dør"));
        Assert.Equal("Ny dør 3", DorNummer.Ledig(["ny dør", "Ny dør 2"], "Ny dør"));
    }

    [Fact]
    public void Likt_IgnorererStoreSmaBokstaverOgMellomrom() =>
        Assert.True(DorNummer.Likt(" yd-02-1-e-k ", "YD-02-1-E-K"));

    [Fact]
    public void Dorfunksjoner_StarAlltidISammeRekkefolge()
    {
        var a = new DorFunksjon { Id = 3, Navn = "Sluttstykke", Forkortelse = "SL" };
        var b = new DorFunksjon { Id = 1, Navn = "Dørautomatikk", Forkortelse = "DA" };
        var c = new DorFunksjon { Id = 2, Navn = "CE-merking", Forkortelse = "CE" };
        var en = new[] { a, b, c }.IFastRekkefolge().Select(f => f.Forkortelse);
        var to = new[] { c, a, b }.IFastRekkefolge().Select(f => f.Forkortelse);
        Assert.Equal(en, to);
    }
}

public class NavigasjonHjelperTests
{
    [Theory]
    [InlineData("prosjekter/12", "/prosjekter")]
    [InlineData("plantegning/5", "/")]
    [InlineData("arbeidsordre/ny", "/arbeidsordre")]
    public void Tilbake_GarAldriTilSideSomIkkeFinnes(string sti, string forventet) =>
        Assert.Equal(forventet, NavigasjonHjelper.FinnForelderSti(sti));
}
