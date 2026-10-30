using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Serviceavtale-PDF-en deler avtaleteksten i seksjoner ved hver overskrift,
// slik at overskriften kan stå i venstre kolonne.
public class ServiceavtalePdfTests
{
    [Fact]
    public void Seksjoner_DelerVedOverskrifter()
    {
        var s = ServiceavtaleTilbudPdfService.Seksjoner("<p>Innledning</p><h2>Omfang</h2><p>A</p><ul><li>B</li></ul><h3>Varighet</h3><p>C</p>");

        Assert.Equal(3, s.Count);
        Assert.Null(s[0].Tittel);
        Assert.Equal("Omfang", s[1].Tittel);
        Assert.Contains("<li>B</li>", s[1].Html);
        Assert.Equal("Varighet", s[2].Tittel);
    }

    [Fact]
    public void Seksjoner_StandardtekstenGirEnSeksjonPerOverskrift()
    {
        var s = ServiceavtaleTilbudPdfService.Seksjoner(ServiceavtalePris.StandardTekst("Firma AS", "Kunde AS", 2));

        Assert.Equal(["Avtalens omfang", "Hva servicebesøket omfatter", "Dette omfattes ikke", "Varighet", "Rapportering og historikk"], s.Select(x => x.Tittel));
    }

    [Fact]
    public void Seksjoner_TomTekstGirIngenSeksjoner() =>
        Assert.Empty(ServiceavtaleTilbudPdfService.Seksjoner("  "));
}
