using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Montører: ingen kassa, ingen sletting, bare lesetilgang på prosjekt.
public class MontorTilgangTests
{
    [Fact]
    public void Montor_HarIkkeKassa_OgKanIkkeSlette()
    {
        Assert.Equal(Rettighet.Ingen, ModulRettigheter.For(BrukerRolle.Montor, "kassa"));
        Assert.False(ModulRettigheter.KanSlette(BrukerRolle.Montor));
        Assert.False(ModulRettigheter.KanSlette(null));
        Assert.True(ModulRettigheter.KanSlette(BrukerRolle.Admin));
        Assert.True(ModulRettigheter.KanSlette(BrukerRolle.Prosjektleder));
        Assert.False(ModulRettigheter.KanRedigere(BrukerRolle.Montor, "prosjekter"));
        Assert.True(ModulRettigheter.KanSetteMontertStatus(BrukerRolle.Montor));
        Assert.True(ModulRettigheter.KanMeldeAvvik(BrukerRolle.Montor));
        Assert.True(ModulRettigheter.KanLeggeTilBilder(BrukerRolle.Montor));
    }
}
