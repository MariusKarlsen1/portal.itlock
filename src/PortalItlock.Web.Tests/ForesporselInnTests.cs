using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Videresendt e-post og automatiske svar i Forespørsler.
public class ForesporselInnTests
{
    [Fact]
    public void Outlook_norsk_videresending_gir_opprinnelig_avsender()
    {
        var tekst = """
            Se under.

            Marius Karlsen
            Itlock AS

            Fra: Kari Hansen <kari@hanseneiendom.no>
            Sendt: onsdag 26. november 2026 09:31
            Til: post@itlock.no
            Emne: Lås på hoveddør går tungt

            Hei, låsen går tungt.
            """;

        var a = ForesporselInn.FinnOpprinneligAvsender("VS: Lås på hoveddør går tungt", tekst, "marius@itlock.no");

        Assert.NotNull(a);
        Assert.Equal("kari@hanseneiendom.no", a!.Epost);
        Assert.Equal("Kari Hansen", a.Navn);
    }

    [Fact]
    public void Gmail_videresending_med_markor_gir_opprinnelig_avsender()
    {
        var tekst = """
            ---------- Forwarded message ---------
            From: Ola Nordmann <Ola@Firma.no>
            Date: Wed, Nov 26, 2026 at 9:31 AM
            Subject: Nøkler
            """;

        var a = ForesporselInn.FinnOpprinneligAvsender("Fwd: Nøkler", tekst, null);

        Assert.Equal("ola@firma.no", a?.Epost);
        Assert.Equal("Ola Nordmann", a?.Navn);
    }

    [Fact]
    public void Outlook_mailto_format_tolkes()
    {
        var a = ForesporselInn.TolkAvsender("\"Kari Hansen\" [mailto:kari@x.no]");
        Assert.Equal("kari@x.no", a?.Epost);
        Assert.Equal("Kari Hansen", a?.Navn);
    }

    [Fact]
    public void Vanlig_epost_som_nevner_fra_er_ikke_videresendt()
    {
        var a = ForesporselInn.FinnOpprinneligAvsender("Spørsmål om lås", "Fra: noen@x.no skrev vi i går", null);
        Assert.Null(a);
    }

    [Fact]
    public void Videresenderens_egen_adresse_hoppes_over()
    {
        var tekst = """
            From: Marius <marius@itlock.no>
            -----Original Message-----
            From: Kunde AS <post@kunde.no>
            """;

        var a = ForesporselInn.FinnOpprinneligAvsender("FW: Tilbud", tekst, "marius@itlock.no");

        Assert.Equal("post@kunde.no", a?.Epost);
    }

    [Theory]
    [InlineData("VS: FW: Fwd: Lås", "Lås")]
    [InlineData("Lås på dør", "Lås på dør")]
    public void RensEmne_fjerner_videresend_prefiks(string emne, string forventet) =>
        Assert.Equal(forventet, ForesporselInn.RensEmne(emne));

    [Theory]
    [InlineData("Automatisk svar: Lås", true)]
    [InlineData("Automatic reply: Lock", true)]
    [InlineData("Fraværende", true)]
    [InlineData("Out of Office: Lock", true)]
    [InlineData("Lås på hoveddør", false)]
    public void Autosvar_kjennes_igjen_pa_emnet(string emne, bool forventet) =>
        Assert.Equal(forventet, ForesporselInn.ErAutosvar(emne, null));
}
