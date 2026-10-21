using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

public class EpostMalTests
{
    private static Tenant Org() => new()
    {
        Navn = "Bryne Låsservice AS",
        ConnectionString = "Data Source=:memory:",
        Adresse = "Storgata 12",
        Postnr = "4340",
        Sted = "Bryne",
        Telefon = "51 00 00 00"
    };

    [Fact]
    public void Lag_HarFirmanavnKnappOgFakta_OgKoderTekst()
    {
        var html = EpostMal.Lag(Org(), new EpostMal.Innhold
        {
            Farge = EpostMal.Gronn,
            Merke = "Ny arbeidsordre",
            Tittel = "Sylinder <b>bytte</b>",
            IntroHtml = "Hei Per, du har fått en ny jobb.",
            Fakta = [("Når", "Torsdag 23. oktober kl. 09:00"), ("Kunde", "Konsmo menighet")],
            Sitat = "Ta med <3 nøkler>",
            KnappTekst = "Åpne arbeidsordren",
            KnappUrl = "https://full-kontroll.no/arbeidsordre/54",
            Lenke2Tekst = "Vis i kart",
            Lenke2Url = EpostMal.KartUrl("Konsmovegen 12"),
            Tips = "Ordren ligger også i Min dag.",
            Grunn = "Du får denne e-posten fordi du er bruker hos Bryne Låsservice AS."
        });

        var tekst = System.Net.WebUtility.HtmlDecode(html);
        Assert.Contains("Bryne Låsservice AS", tekst);
        Assert.Contains("https://full-kontroll.no/arbeidsordre/54", html);
        Assert.Contains("Konsmo menighet", tekst);
        Assert.Contains("Sylinder &lt;b&gt;bytte&lt;/b&gt;", html);
        Assert.Contains("Ta med &lt;3 n", html);
        Assert.Contains("Storgata 12, 4340 Bryne", tekst);
        Assert.Contains("Levert av Full Kontroll", html);
        Assert.DoesNotContain("<b>bytte</b>", html);

        // Lagrer et eksempel for å se på e-posten i nettleseren.
        if (Environment.GetEnvironmentVariable("EPOSTMAL_UT") is { Length: > 0 } fil)
        {
            File.WriteAllText(fil, html);
        }
    }

    [Fact]
    public void Telefon_GirRingeLenkeUtenMellomrom()
    {
        Assert.Contains("href=\"tel:90012345\"", EpostMal.Telefon("900 12 345"));
        Assert.Equal("", EpostMal.Telefon(null));
    }
}
