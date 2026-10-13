using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Fester tilgangsmatrisen slik den ble spesifisert 2026-10-13. Endrer noen
// reglene i ModulRettigheter uten å mene det, faller testene her.
public class ModulRettigheterTests
{
    [Theory]
    [InlineData("prosjekter")]
    [InlineData("arbeidsordre")]
    [InlineData("tilbud-service")]
    [InlineData("kalender")]
    [InlineData("timegodkjenning")]
    [InlineData("guide/kobling")]
    [InlineData("oppsettmodul")]
    [InlineData("komponenter")]
    public void Admin_HarFullTilgangTilAlt(string modul)
    {
        Assert.Equal(Rettighet.Full, ModulRettigheter.For(BrukerRolle.Admin, modul));
    }

    [Fact]
    public void Prosjektleder_HarAltUnntattAdministrasjon()
    {
        // Administrasjon er sperret ved at modulene ikke er synlige for dem -
        // kategorien faller bort i KategorierFor, så de kommer aldri inn.
        Assert.DoesNotContain(
            PortalModulRegister.KategorierFor(BrukerRolle.Prosjektleder),
            k => k.Nokkel == "administrasjon");

        var synlige = PortalModulRegister.BladLenkerFor(BrukerRolle.Prosjektleder).Select(l => l.Href);
        foreach (var modul in synlige)
        {
            Assert.Equal(Rettighet.Full, ModulRettigheter.For(BrukerRolle.Prosjektleder, modul));
        }
    }

    // ---------- Montør ----------

    [Theory]
    // Ressurser og godkjenninger: bare se.
    [InlineData("kalender", Rettighet.Les)]
    [InlineData("timegodkjenning", Rettighet.Les)]
    // Teknisk og ADK: bare se - bortsett fra Kobling, der de kan lage nytt.
    [InlineData("guide/kobling", Rettighet.Opprett)]
    [InlineData("dormiljo", Rettighet.Les)]
    [InlineData("guide/oppsett-adgangskontroll", Rettighet.Les)]
    [InlineData("systemregister", Rettighet.Les)]
    // Montørverktøy: full tilgang.
    [InlineData("min-dag", Rettighet.Full)]
    [InlineData("befaringsliste", Rettighet.Full)]
    [InlineData("lasutskifting", Rettighet.Full)]
    [InlineData("lasekasse-utskiftning", Rettighet.Full)]
    [InlineData("abc-kalkulator", Rettighet.Full)]
    // Prosjekt, service og tilbud.
    [InlineData("prosjekter", Rettighet.Les)]
    [InlineData("arbeidsordre", Rettighet.Les)]
    [InlineData("finn-dorpakke", Rettighet.Les)]
    [InlineData("ce-godkjenninger", Rettighet.Les)]
    [InlineData("tilbud-service", Rettighet.Ingen)]
    // Egne timer og fravær.
    [InlineData("timeregistrering", Rettighet.Full)]
    [InlineData("fravar", Rettighet.Full)]
    // Kunder, drift og administrasjon: ingenting.
    [InlineData("kunder", Rettighet.Ingen)]
    [InlineData("service", Rettighet.Ingen)]
    [InlineData("tickets", Rettighet.Ingen)]
    [InlineData("foresporsler", Rettighet.Ingen)]
    [InlineData("oppsettmodul", Rettighet.Ingen)]
    [InlineData("brukere", Rettighet.Ingen)]
    public void Montor_HarRiktigNivaa(string modul, Rettighet forventet)
    {
        Assert.Equal(forventet, ModulRettigheter.For(BrukerRolle.Montor, modul));
    }

    [Fact]
    public void Montor_SerIkkeTilbudService()
    {
        var synlige = PortalModulRegister.BladLenkerFor(BrukerRolle.Montor).Select(l => l.Href).ToHashSet();
        Assert.DoesNotContain("tilbud-service", synlige);
    }

    [Fact]
    public void Montor_SerAlleModuleneDeHarRettighetTil()
    {
        // Matrisen og synligheten må være enige: har montøren en rettighet til
        // en modul, skal modulen også ligge i menyen - ellers er rettigheten
        // umulig å bruke.
        var synlige = PortalModulRegister.BladLenkerFor(BrukerRolle.Montor).Select(l => l.Href).ToHashSet();

        foreach (var href in synlige)
        {
            Assert.True(
                ModulRettigheter.KanSe(BrukerRolle.Montor, href),
                $"{href} er synlig for montør, men matrisen gir ingen rettighet.");
        }
    }

    [Fact]
    public void Montor_KanBareSeIkkeRedigere_PaaLesemodulene()
    {
        foreach (var modul in new[] { "kalender", "timegodkjenning", "dormiljo", "systemregister", "prosjekter", "arbeidsordre", "ce-godkjenninger", "finn-dorpakke" })
        {
            Assert.True(ModulRettigheter.KanSe(BrukerRolle.Montor, modul), modul);
            Assert.False(ModulRettigheter.KanRedigere(BrukerRolle.Montor, modul), modul);
            Assert.False(ModulRettigheter.KanOpprette(BrukerRolle.Montor, modul), modul);
        }
    }

    [Fact]
    public void Montor_KanLageNyttKoblingsskjemaMenIkkeEndreEksisterende()
    {
        Assert.True(ModulRettigheter.KanOpprette(BrukerRolle.Montor, "guide/kobling"));
        Assert.False(ModulRettigheter.KanRedigere(BrukerRolle.Montor, "guide/kobling"));
    }

    [Fact]
    public void Montor_HarDeNavngitteUnntakene()
    {
        Assert.True(ModulRettigheter.KanForeTimer(BrukerRolle.Montor));
        Assert.True(ModulRettigheter.KanSetteMontertStatus(BrukerRolle.Montor));
        Assert.True(ModulRettigheter.KanMeldeAvvik(BrukerRolle.Montor));
        Assert.True(ModulRettigheter.KanLeggeTilBilder(BrukerRolle.Montor));
    }

    [Fact]
    public void Montor_SerIkkeTilbudVedleggEllerMonteringsfanen()
    {
        var skjulte = ModulRettigheter.SkjulteProsjektFaner(BrukerRolle.Montor);

        Assert.Contains("tilbud", skjulte);
        Assert.Contains("vedlegg", skjulte);
        Assert.Contains("montering", skjulte);

        Assert.Empty(ModulRettigheter.SkjulteProsjektFaner(BrukerRolle.Admin));
        Assert.Empty(ModulRettigheter.SkjulteProsjektFaner(BrukerRolle.Prosjektleder));
    }

    [Fact]
    public void UtenRolle_HarIngenting()
    {
        Assert.Equal(Rettighet.Ingen, ModulRettigheter.For(null, "prosjekter"));
        Assert.False(ModulRettigheter.KanSe(null, "kalender"));
    }
}
