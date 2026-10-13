namespace PortalItlock.Web.Models;

// Hva en rolle får GJØRE i en modul - i motsetning til ModulTilgang, som bare
// avgjør om modulen er synlig i menyen og på forsiden.
//
// De to henger sammen slik: ModulTilgang på hver Lenke (se PortalModulRegister)
// bestemmer hvem som SER modulen, og Rettighet her bestemmer om de kan endre
// noe når de er inne. En modul som ikke er synlig gir alltid Ingen.
public enum Rettighet
{
    Ingen = 0,

    // Kan åpne og lese, men ikke lagre, endre eller slette noe.
    Les = 1,

    // Kan lese og opprette nytt, men ikke endre eller slette det som finnes
    // fra før. Brukes av Kobling for montører.
    Opprett = 2,

    // Full tilgang: lese, opprette, endre og slette.
    Full = 3
}

// Fasiten for hva hver rolle får gjøre hvor. Alt som håndhever tilgang i
// portalen skal spørre denne klassen, ikke gjenta reglene selv - da står
// reglene på ett sted og kan testes (se PortalModulRegisterTests).
//
// Hovedreglene:
//   Admin          - alt
//   Prosjektleder  - alt unntatt Administrasjon
//   Montør         - se detaljmatrisen under
public static class ModulRettigheter
{
    // Nøklene er Lenke.Href fra PortalModulRegister, altså modulens egen
    // adresse. Bare montører trenger en matrise; de to andre rollene følger
    // hovedregelen over.
    private static readonly Dictionary<string, Rettighet> MontorRettigheter = new()
    {
        // Ressurser og godkjenninger - bare se.
        ["kalender"] = Rettighet.Les,
        ["timegodkjenning"] = Rettighet.Les,

        // Teknisk og ADK - bare se. Kobling er unntaket: der kan de lage nye
        // skjema, men ikke endre de som ligger der fra før.
        ["guide/kobling"] = Rettighet.Opprett,
        ["dormiljo"] = Rettighet.Les,
        ["guide/oppsett-adgangskontroll"] = Rettighet.Les,
        ["systemregister"] = Rettighet.Les,

        // Montørverktøy - full tilgang, dette er verktøyene deres.
        ["min-dag"] = Rettighet.Full,
        ["befaringsliste"] = Rettighet.Full,
        ["lasutskifting"] = Rettighet.Full,
        ["lasekasse-utskiftning"] = Rettighet.Full,
        ["abc-kalkulator"] = Rettighet.Full,

        // Prosjekt, service og tilbud.
        // Prosjekter og arbeidsordre er lesetilgang med noen få unntak, se
        // de navngitte reglene nederst i klassen.
        ["prosjekter"] = Rettighet.Les,
        ["arbeidsordre"] = Rettighet.Les,
        ["finn-dorpakke"] = Rettighet.Les,
        ["ce-godkjenninger"] = Rettighet.Les,
        // Tilbud service er bevisst ikke med: montører skal ikke se tilbud.

        // Egne timer og fravær fører de selv.
        ["timeregistrering"] = Rettighet.Full,
        ["fravar"] = Rettighet.Full
    };

    public static Rettighet For(BrukerRolle? rolle, string modul) => rolle switch
    {
        BrukerRolle.Admin => Rettighet.Full,

        // Prosjektledere har alt unntatt Administrasjon. Hvilke moduler som
        // ligger under Administrasjon avgjøres av ModulTilgang.KunAdmin i
        // registeret, så her trengs ingen egen liste - er modulen ikke synlig
        // for dem, kommer de aldri inn på den.
        BrukerRolle.Prosjektleder => Rettighet.Full,

        BrukerRolle.Montor => MontorRettigheter.GetValueOrDefault(modul, Rettighet.Ingen),

        _ => Rettighet.Ingen
    };

    public static bool KanSe(BrukerRolle? rolle, string modul) => For(rolle, modul) >= Rettighet.Les;

    public static bool KanOpprette(BrukerRolle? rolle, string modul) => For(rolle, modul) >= Rettighet.Opprett;

    public static bool KanRedigere(BrukerRolle? rolle, string modul) => For(rolle, modul) == Rettighet.Full;

    // ---------- Navngitte unntak ----------
    //
    // Montører er i hovedsak lesere på prosjekt og arbeidsordre, men skal
    // likevel kunne gjøre nøyaktig disse tingene. Reglene står her, ett sted,
    // i stedet for spredt som "if rolle == Montor" rundt i komponentene.

    // Timer føres av alle - det er montørens egen jobb som registreres.
    public static bool KanForeTimer(BrukerRolle? rolle) => rolle is not null;

    // Huke av montert/ikke montert på dørene i et prosjekt.
    public static bool KanSetteMontertStatus(BrukerRolle? rolle) =>
        rolle is BrukerRolle.Admin or BrukerRolle.Prosjektleder or BrukerRolle.Montor;

    // Melde inn avvik fra felt.
    public static bool KanMeldeAvvik(BrukerRolle? rolle) =>
        rolle is BrukerRolle.Admin or BrukerRolle.Prosjektleder or BrukerRolle.Montor;

    // Laste opp bilder på et prosjekt.
    public static bool KanLeggeTilBilder(BrukerRolle? rolle) =>
        rolle is BrukerRolle.Admin or BrukerRolle.Prosjektleder or BrukerRolle.Montor;

    // Ferdigmelde en arbeidsordre. Montøren er den som faktisk gjør jobben og
    // lukker den ute i felt, så dette er et unntak fra lesetilgangen på
    // arbeidsordre. Å markere som FAKTURERT er derimot ikke med - det er en
    // økonomihandling og krever full tilgang.
    public static bool KanFerdigmelde(BrukerRolle? rolle) =>
        rolle is BrukerRolle.Admin or BrukerRolle.Prosjektleder or BrukerRolle.Montor;

    // Fanene på prosjektsiden en rolle ikke skal se i det hele tatt.
    public static IReadOnlySet<string> SkjulteProsjektFaner(BrukerRolle? rolle) =>
        rolle == BrukerRolle.Montor
            ? new HashSet<string> { "tilbud", "vedlegg", "montering", "personer" }
            : new HashSet<string>();
}
