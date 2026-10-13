using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Forsiden ble bygget om fra tre rolleseksjoner ("For montører", "For admin",
// "For prosjektledere") til én felles modulliste med kategorier. Kravet var at
// INGEN skal miste tilgang til noe de hadde fra før: en montør skal se nøyaktig
// det samme som før, en prosjektleder det samme, og admin alt.
//
// Disse testene holder det kravet på plass. Den gamle rollelista (Roller) ligger
// fortsatt i registeret som fasit og for bakoverkompatible "?rolle="-lenker;
// hvis noen senere fjerner en modul fra kategoriene uten å fjerne den fra den
// gamle lista, faller testen her.
public class PortalModulRegisterTests
{
    // Nøyaktig samme rolle-til-seksjon-oppslag som den gamle venstremenyen
    // brukte før ombyggingen (se NavMenu/TopBar før 2026-10-12).
    private static readonly Dictionary<BrukerRolle, string[]> GammelMenyPerRolle = new()
    {
        [BrukerRolle.Montor] = ["montorer"],
        [BrukerRolle.Prosjektleder] = ["montorer", "prosjektledere"],
        [BrukerRolle.Admin] = ["montorer", "prosjektledere", "admin"]
    };

    private static HashSet<string> GamleModulerFor(BrukerRolle rolle) =>
        PortalModulRegister.Roller
            .Where(r => GammelMenyPerRolle[rolle].Contains(r.Nokkel))
            .SelectMany(r => r.Grupper)
            .SelectMany(g => g.Lenker)
            .SelectMany(BladeneI)
            .Select(l => l.Href)
            .ToHashSet();

    private static IEnumerable<Lenke> BladeneI(Lenke lenke) =>
        lenke.Undermoduler is { Count: > 0 } under ? under : [lenke];

    // Moduler en rolle BEVISST har fått i tillegg etter ombyggingen. Alt annet
    // som dukker opp får testen under til å feile, slik at ingen får utvidet
    // tilgang ved et uhell.
    //
    // Montørene fikk lesetilgang til disse 2026-10-13 på uttrykkelig ønske:
    // hele Teknisk og ADK og hele Ressurser og godkjenninger skal de kunne se,
    // og Finn dørpakke likeså. Hva de får GJØRE der styres av
    // ModulRettigheter (se ModulRettigheterTests).
    private static readonly Dictionary<BrukerRolle, string[]> BevisstLagtTil = new()
    {
        [BrukerRolle.Montor] = ["finn-dorpakke", "systemregister", "timegodkjenning"],
        [BrukerRolle.Prosjektleder] = [],
        [BrukerRolle.Admin] = []
    };

    // Moduler som bevisst er tatt HELT ut av portalen. Sidene lever videre på
    // sine adresser, men ligger ikke i menyen lenger.
    private static readonly string[] BevisstFjernet = ["nyheter", "notater"];

    [Theory]
    [InlineData(BrukerRolle.Montor)]
    [InlineData(BrukerRolle.Prosjektleder)]
    [InlineData(BrukerRolle.Admin)]
    public void NyeKategorier_GirSammeModulerSomFor_UtenomDeBevisstEndrede(BrukerRolle rolle)
    {
        var gamle = GamleModulerFor(rolle);
        var nye = PortalModulRegister.BladLenkerFor(rolle).Select(l => l.Href).ToHashSet();

        var mistet = gamle.Except(nye).Except(BevisstFjernet).OrderBy(h => h).ToList();
        var nyeEkstra = nye.Except(gamle).Except(BevisstLagtTil[rolle]).OrderBy(h => h).ToList();

        Assert.True(mistet.Count == 0, $"{rolle} mistet tilgang til: {string.Join(", ", mistet)}");
        Assert.True(nyeEkstra.Count == 0, $"{rolle} fikk nye moduler den ikke hadde før: {string.Join(", ", nyeEkstra)}");
    }

    [Fact]
    public void Montor_SerIkkeModulerSomKreverProsjektlederEllerAdmin()
    {
        var montor = PortalModulRegister.BladLenkerFor(BrukerRolle.Montor).Select(l => l.Href).ToHashSet();

        // Moduler montører fortsatt ikke skal se i det hele tatt. Merk at
        // timegodkjenning IKKE står her lenger - den ser de nå, men bare som
        // lesetilgang (se ModulRettigheterTests).
        foreach (var sperret in new[] { "tickets", "kunder", "service", "foresporsler", "tilbud-service", "brukere", "komponenter" })
        {
            Assert.DoesNotContain(sperret, montor);
        }
    }

    [Fact]
    public void Administrasjon_ErKunForAdmin()
    {
        var adminKategori = PortalModulRegister.Kategorier.Single(k => k.Nokkel == "administrasjon");

        foreach (var lenke in adminKategori.Grupper.SelectMany(g => g.Lenker))
        {
            Assert.Equal(ModulTilgang.KunAdmin, lenke.Tilgang);
        }

        Assert.DoesNotContain(PortalModulRegister.KategorierFor(BrukerRolle.Montor), k => k.Nokkel == "administrasjon");
        Assert.DoesNotContain(PortalModulRegister.KategorierFor(BrukerRolle.Prosjektleder), k => k.Nokkel == "administrasjon");
        Assert.Contains(PortalModulRegister.KategorierFor(BrukerRolle.Admin), k => k.Nokkel == "administrasjon");
    }

    // Moduler merket KunIMeny skal fortsatt ligge i venstremenyen - de er bare
    // tatt ut av kategorikortene på forsiden. Blir de borte fra menyen også,
    // har noen mistet en modul uten å mene det.
    [Theory]
    [InlineData(BrukerRolle.Montor)]
    [InlineData(BrukerRolle.Prosjektleder)]
    [InlineData(BrukerRolle.Admin)]
    public void KunIMenyModuler_LiggerIMenyenMenIkkePaKategorikortene(BrukerRolle rolle)
    {
        var kunIMeny = PortalModulRegister.Kategorier
            .SelectMany(k => k.Grupper)
            .SelectMany(g => g.Lenker)
            .Where(l => l.KunIMeny && l.Tilgang.HasFlag(TilgangFor(rolle)))
            .Select(l => l.Href)
            .ToList();

        Assert.NotEmpty(kunIMeny);

        var meny = PortalModulRegister.ModulerFor(rolle).Select(l => l.Href).ToHashSet();
        var kort = PortalModulRegister.KategorierFor(rolle)
            .SelectMany(k => k.Grupper)
            .SelectMany(g => g.Lenker)
            .Select(l => l.Href)
            .ToHashSet();

        foreach (var href in kunIMeny)
        {
            Assert.Contains(href, meny);
            Assert.DoesNotContain(href, kort);
        }
    }

    private static ModulTilgang TilgangFor(BrukerRolle rolle) => rolle switch
    {
        BrukerRolle.Admin => ModulTilgang.Admin,
        BrukerRolle.Prosjektleder => ModulTilgang.Prosjektleder,
        _ => ModulTilgang.Montor
    };

    [Fact]
    public void IngenKategorilenke_PekerPaEnHubUtenUndermoduler()
    {
        // Hub-nøkler ("oppsettmodul") har ingen egen side - de skal alltid ha
        // undermoduler, ellers blir kortet en blindvei.
        foreach (var lenke in PortalModulRegister.Kategorier.SelectMany(k => k.Grupper).SelectMany(g => g.Lenker))
        {
            if (lenke.Href.EndsWith("modul", StringComparison.Ordinal))
            {
                Assert.True(lenke.Undermoduler is { Count: > 0 }, $"{lenke.Tittel} er en hub uten undermoduler.");
            }
        }
    }
}
