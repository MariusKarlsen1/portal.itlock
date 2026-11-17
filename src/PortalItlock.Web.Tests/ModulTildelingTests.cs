using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// En montør kan få «Komponenter og priser» i tillegg til rollen sin.
public class ModulTildelingTests
{
    [Fact]
    public void Montor_ser_ikke_komponentmodulen_uten_tildeling() =>
        Assert.DoesNotContain(PortalModulRegister.ModulerFor(BrukerRolle.Montor), l => l.Href == ModulTildeling.KomponentPris);

    [Fact]
    public void Montor_ser_komponentmodulen_med_tildeling() =>
        Assert.Contains(PortalModulRegister.ModulerFor(BrukerRolle.Montor, null, ModulTildeling.Les("komponentprismodul")), l => l.Href == ModulTildeling.KomponentPris);

    [Fact]
    public void Ukjente_moduler_kan_ikke_tildeles()
    {
        Assert.Empty(ModulTildeling.Les("brukere,oppsettmodul"));
        Assert.Null(ModulTildeling.Skriv(["brukere"]));
        Assert.Equal("komponentprismodul", ModulTildeling.Skriv(["komponentprismodul", "brukere", "komponentprismodul"]));
    }
}
