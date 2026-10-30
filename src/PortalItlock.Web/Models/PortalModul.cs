namespace PortalItlock.Web.Models;

// Hvem som ser en modul. Verdiene er utledet fra de tre gamle seksjonene på
// forsiden, slik at ingen mister eller får noe de ikke hadde: en modul som lå
// under "For montører" ble vist til montør, prosjektleder og admin, en under
// "For prosjektledere" til prosjektleder og admin, og en under "For admin"
// bare til admin (se NavMenu/TopBar sin gamle nøkkel-liste).
//
// Dette styrer hva som VISES. Selve tilgangen ligger fortsatt på hver side,
// i [Authorize(Roles = ...)], og den er ikke rørt.
[Flags]
public enum ModulTilgang
{
    Ingen = 0,
    Montor = 1,
    Prosjektleder = 2,
    Admin = 4,
    Alle = Montor | Prosjektleder | Admin,
    FraProsjektleder = Prosjektleder | Admin,
    KunAdmin = Admin
}

public sealed record Lenke(
    string Tittel,
    string Beskrivelse,
    string Href,
    string IconNavn = "folder",
    List<Lenke>? Undermoduler = null,
    ModulTilgang Tilgang = ModulTilgang.Alle,
    // Modulen ligger i venstremenyen som vanlig, men vises ikke som kort i
    // kategorien på forsiden - for moduler man uansett går rett til fra
    // menyen, og som bare ville tatt plass i kategorioversikten.
    bool KunIMeny = false,
    // Tilleggsmodul organisasjonen må ha kjøpt (TilleggsModuler), f.eks. "lager".
    // Uten den vises lenken ikke - heller ikke når listen over aktive
    // moduler ikke er sendt med (trygg standard).
    string? KreverModul = null);

public sealed record LenkeGruppe(string Navn, List<Lenke> Lenker);
public sealed record Rolle(string Nokkel, string Tittel, string Beskrivelse, List<LenkeGruppe> Grupper);

public static class PortalModulRegister
{
    private static readonly List<Lenke> BefaringUndermoduler =
    [
        new Lenke("Befaringsliste", "Sjekkliste og notater fra befaring.", "befaringsliste", "check-circle"),
        new Lenke("Utskiftning av lås", "Sjekkliste med mål på dør, karm og hengsling.", "lasutskifting", "tool"),
        new Lenke("Utskiftning av låskasse", "Søk opp gammel lås, se hva du trenger.", "lasekasse-utskiftning", "tool"),
        new Lenke("ABC-kalkulator", "Sylinderforlenger, skilt og skruer fra A/B/C-mål.", "abc-kalkulator", "tool"),
    ];

    private static readonly List<Lenke> GuiderUndermoduler =
    [
        new Lenke("Kobling", "Guide for kobling.", "guide/kobling", "trend-up"),
        new Lenke("Dørmiljø", "Tegninger til bruk på befaring og i prosjektering.", "dormiljo", "box"),
        new Lenke("Oppsett adgangskontroll", "Guide for oppsett av adgangskontroll.", "guide/oppsett-adgangskontroll", "bell"),
        // Regelverk-guiden er bevisst tatt ut av menyen/portalen på ønske
        // 2026-10-10, men selve siden lever videre på /guide/regelverk
        // (GuideSideVisning + tittelen i Titler-oppslaget der) slik at
        // innholdet er intakt og lenken fra CE-teksten i ProsjektSkjema
        // fortsatt virker. Legg linjen under tilbake her for å vise den igjen:
        // new Lenke("Regelverk", "TEK17-brannkrav, maskindirektivet og NS-EN 16005 for dørautomatikk - kilder og huskeliste.", "guide/regelverk", "shield"),
    ];

    private static readonly List<Lenke> ProsjektMontorUndermoduler =
    [
        new Lenke("Prosjekter", "Prosjekter du er lagt til på.", "prosjekter", "folder"),
        new Lenke("Arbeidsordre", "Se arbeidsordre med info om jobben og ansvarlig montør.", "arbeidsordre", "receipt"),
        new Lenke("CE-godkjenninger", "CE-sertifiseringer for dører med automatikk, på tvers av prosjekter.", "ce-godkjenninger", "check-circle", KreverModul: TilleggsModuler.Ce),
    ];

    private static readonly List<Lenke> TimerFravarUndermoduler =
    [
        new Lenke("Fravær", "Søk om ferie eller meld fravær.", "fravar", "sun", KreverModul: TilleggsModuler.Timer),
        new Lenke("Timeregistrering", "Registrer timer på en arbeidsordre.", "timeregistrering", "clock", KreverModul: TilleggsModuler.Timer),
    ];

    private static readonly List<Lenke> ProsjekteringUndermoduler =
    [
        new Lenke("Prosjekter", "Dører, plantegninger og montasjestatus per prosjekt.", "prosjekter", "folder"),
        new Lenke("Arbeidsordre", "Opprett arbeidsordre med info om jobben og ansvarlig montør.", "arbeidsordre", "check-circle"),
        new Lenke("Finn dørpakke", "Huk av krav og finn riktig dørpakke til prosjektet.", "finn-dorpakke", "box"),
        new Lenke("Systemregister", "Søk opp låssystem, kunde, adresse og rekvirenter.", "systemregister", "users"),
        new Lenke("CE Modul", "Alle CE-sertifiseringer på tvers av prosjekter, med status, vilkår og gyldighet.", "ce-godkjenninger", "shield", KreverModul: TilleggsModuler.Ce),
    ];

    private static readonly List<Lenke> KunderUndermoduler =
    [
        new Lenke("Kunderegister", "Opprett og vedlikehold kunder med kontaktinfo og adresse.", "kunder", "users"),
    ];

    private static readonly List<Lenke> DriftUndermoduler =
    [
        new Lenke("Kalender", "Se planlagte arbeidsordre for alle montører.", "kalender", "calendar", KreverModul: TilleggsModuler.Timer),
        new Lenke("Fravær", "Søknader, godkjenning og fraværskalender per person.", "fravar", "sun", KreverModul: TilleggsModuler.Timer),
        new Lenke("Forespørsler", "E-poster sendt til post@itlock.no.", "foresporsler", "mail"),
    ];

    private static readonly List<Lenke> ServiceUndermoduler =
    [
        new Lenke("Serviceavtaler", "Serviceavtaler og serviceoppdrag samlet, med servicehistorikk og rapporter.", "service", "clock"),
        new Lenke("Tilbud", "Opprett og se tilbud for serviceavtaler, med estimater fra arbeidsordre-minuttene.", "tilbud-service", "receipt"),
    ];

    private static readonly List<Lenke> OppsettUndermoduler =
    [
        new Lenke("Dørpakker", "Sett sammen dørpakker, eller rediger og slett eksisterende.", "dorpakker", "box"),
        new Lenke("Kravdimensjoner", "Vedlikehold kravkategoriene kravvelgeren bygger på.", "kravdimensjoner", "check-circle"),
        new Lenke("Dørfunksjoner", "Opprett og vedlikehold dørfunksjoner som kan legges på dører.", "dorfunksjoner", "tool"),
        new Lenke("Sjekklistemaler", "Digitale sjekklister per dørtype til bruk på arbeidsordre.", "sjekklistemaler", "alert"),
        new Lenke("Sjekkliste for servicerunder", "Punktene som automatisk legges til hver servicerunde.", "service-sjekklistemal", "clock"),
        new Lenke("Tilvalgsmaler", "Ferdige maler med alternativer, bilder og priser til gjenbruk.", "tilvalgmaler", "receipt"),
        new Lenke("Forsider", "Lag forside-/forbeholdstekster til bruk på tilbud.", "forsider", "folder"),
        new Lenke("Godkjenninger", "Godkjenn innsendte timer og fraværssøknader, se sammendrag og eksporter til lønn.", "timegodkjenning", "check-circle", KreverModul: TilleggsModuler.Timer),
        new Lenke("CE-sertifisering", "Sett CE-kategori og standarddokument på beslagstyper, og grenseverdier for mål.", "ce-oppsett", "check-circle"),
        // Hjemmesidetekster, Nyheter og Notater er tatt ut av menyen/portalen
        // på ønske 2026-10-13. Hjemmesidetekster redigerte tittel/ingress og
        // de tre rolle-tekstene på gamle forsiden - rollekortene er borte, så
        // feltene var uten virkning. Sidene lever videre på
        // /hjemmesidetekster, /nyheter og /notater, og ingen data er rørt.
        // Legg linjene under tilbake her for å vise dem igjen:
        // new Lenke("Hjemmesidetekster", "Rediger overskrift, ingress og rolle-tekstene på forsiden.", "hjemmesidetekster", "users"),
        // new Lenke("Nyheter", "Se automatisk genererte \"Hva er nytt\"-oppdateringer som vises på forsiden til alle brukere.", "nyheter", "bell"),
        // new Lenke("Notater", "Alle notater ansatte har skrevet i portalen, samlet på ett sted.", "notater", "file-text"),
    ];

    private static readonly List<Lenke> KomponentPrisUndermoduler =
    [
        new Lenke("Komponentregister", "Søk, prissett og administrer komponenter og leverandører.", "komponenter", "tool"),
        new Lenke("Prisimport", "Oppdater prisene fra leverandørens prisliste, eller importer nye varer.", "komponenter/prisimport", "receipt"),
        new Lenke("Leverandører", "Søk opp leverandører, se hvilke varer de leverer, og sett varenummer/pris/standard pr. leverandør.", "komponenter/leverandorer", "users"),
        new Lenke("Rabattgrupper", "Faste rabattavtaler per leverandør/produktserie, brukes til å beregne nettopris automatisk.", "rabattgrupper", "tag"),
        new Lenke("Produktgrupper", "Opprett produktgrupper og legg til varer i dem. Brukes til å sette kunderabatt per gruppe.", "produktgrupper", "folder"),
    ];

    private static readonly List<Lenke> BrukerKundeUndermoduler =
    [
        new Lenke("Brukere", "Opprett brukere med e-post, stilling og tilgang til portalen.", "brukere", "users"),
        new Lenke("Kundetilganger", "Administrer innlogging til kundeportalen.", "kundetilganger", "bell"),
        // Fravær er tatt ut herfra 2026-10-13 - det hører under Ressurser og
        // godkjenninger, der det allerede ligger, og trengs ikke to steder.
        new Lenke("Driftslisenser", "Gi vaktmestere tilgang til å registrere driftsmeldinger på utvalgte prosjekter.", "driftslisenser", "tool"),
    ];

    private static readonly List<Lenke> RapporterUndermoduler =
    [
        new Lenke("Kunde", "Topp 20 kunder etter omsetning, for valgt periode (portalens egne data).", "rapporter/kunde", "users"),
        new Lenke("Produkt", "Salg pr. vare, med mulighet for å filtrere på periode og kunde.", "rapporter/produkt", "box"),
        new Lenke("Resultatrapport", "Omsetning pr. inntektskonto for valgt periode.", "rapporter/resultat", "receipt"),
        new Lenke("Kontoplan", "Opprett og vedlikehold inntektskontoene som kan settes på varer.", "rapporter/kontoplan", "settings"),
        new Lenke("Fakturaoversikt", "Fakturaer sendt fra Tripletex, hentet direkte derfra.", "rapporter/fakturaer", "receipt"),
        new Lenke("Salg per kunde", "Faktisk fakturert salg pr. kunde, hentet direkte fra Tripletex.", "rapporter/salg-per-kunde", "users"),
        new Lenke("Tilbudsreserve", "Alle tilbud sendt til kunde, med dato og sum, og total tilbudsreserve som fortsatt venter på svar.", "rapporter/tilbudsreserve", "receipt"),
    ];

    public static readonly List<Rolle> Roller =
    [
        new Rolle(
            "montorer",
            "For montører",
            "Guide, befaring og vedlegg til bruk i felt",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Befaring", "Befaringsliste, utskiftning av lås og låskasse, og ABC-kalkulator", "befaringsmodul", "eye", BefaringUndermoduler),
                    new Lenke("Guider", "Kobling, dørmiljø og oppsett adgangskontroll", "guidermodul", "tool", GuiderUndermoduler),
                    new Lenke("Prosjekt", "Prosjekter og arbeidsordre", "prosjektmodul", "folder", ProsjektMontorUndermoduler),
                    new Lenke("Timer og fravær", "Fravær og timeregistrering", "timerfravarmodul", "clock", TimerFravarUndermoduler, KreverModul: TilleggsModuler.Timer),
                ]),
                new LenkeGruppe("Kart og kalender", [
                    new Lenke("Min dag", "Dagens jobber, adresse og varer å pakke, samlet", "min-dag", "calendar"),
                    // Kart er tatt ut for montører: /kart viser nøyaktig samme
                    // visning som Kart-fanen på Arbeidsordre (begge bruker
                    // ArbeidsordreKartvisning), så det var to veier til det
                    // samme. Siden ligger fortsatt under Drift.
                    // new Lenke("Kart", "Se dagens jobber geografisk", "kart", "map"),
                    new Lenke("Kalender", "Se planlagte arbeidsordre", "kalender", "calendar", KreverModul: TilleggsModuler.Timer),
                ]),
            ]),
        new Rolle(
            "prosjektledere",
            "For prosjektledere",
            "Systemer, prosjektering og prosjekter",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Prosjekt modul", "Prosjekter, dørpakker og systemregister samlet på ett sted", "prosjekteringmodul", "folder", ProsjekteringUndermoduler),
                    new Lenke("Kunder", "Kunderegister, oppfølging, portaltilgang og tilvalg samlet på ett sted", "kundemodul", "users", KunderUndermoduler),
                    new Lenke("Drift", "Kart, kalender og fravær samlet på ett sted", "driftmodul", "map", DriftUndermoduler),
                    new Lenke("Forespørsler", "E-poster sendt til post@itlock.no", "foresporsler", "mail"),
                    new Lenke("Tickets", "Samlet oversikt over alle henvendelser og saker", "tickets", "tag"),
                    new Lenke("Service modul", "Serviceavtaler, serviceoppdrag og tilbud service samlet på ett sted", "servicemodul", "clock", ServiceUndermoduler),
                    // Prosjektlederne godkjenner timene og fraværssøknadene til
                    // montørene sine, og henter rapporten derfra.
                    new Lenke("Godkjenninger", "Alle innsendte timer og fraværssøknader til godkjenning, med sammendrag og rapport", "timegodkjenning", "check-circle", KreverModul: TilleggsModuler.Timer),
                ]),
            ]),
        new Rolle(
            "admin",
            "For admin",
            "Opprett og vedlikehold grunnlagsdata",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Oppsett og maler", "Dørpakker, krav, sjekklister, tilvalg og dokumenter samlet på ett sted", "oppsettmodul", "settings", OppsettUndermoduler),
                    new Lenke("Komponenter og priser", "Komponentregister, prisimport, leverandører, rabattgrupper og produktgrupper samlet på ett sted", "komponentprismodul", "box", KomponentPrisUndermoduler),
                    new Lenke("Brukere og kunder", "Brukere, kundetilgang og driftslisenser samlet på ett sted", "brukerkundemodul", "users", BrukerKundeUndermoduler),
                    new Lenke("Rapporter", "Kunde-, produkt- og resultatrapport beregnet fra egne arbeidsordre, pluss kontoplan", "rapportermodul", "receipt", RapporterUndermoduler),
                ]),
            ]),
    ];

    // ---------- Kategoriene på den nye forsiden ----------
    //
    // Én felles liste for alle, i stedet for tre rolleknapper. Modulene er de
    // samme som før, bare gruppert etter hva de brukes til; Tilgang på hver
    // enkelt avgjør hvem som ser den. Administrasjon er sin egen kategori og
    // blandes ikke inn i de andre.
    public static readonly List<Rolle> Kategorier =
    [
        new Rolle(
            "prosjekt",
            "Prosjekt, service og tilbud",
            "Prosjekter, jobber, tilbud og service samlet",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Prosjekter", "Dører, tegninger og status per prosjekt.", "prosjekter", "folder"),
                    new Lenke("Arbeidsordre", "Jobber, timer og status samlet.", "arbeidsordre", "check-circle"),
                    new Lenke("Finn dørpakke", "Velg krav og få riktig dørpakke.", "finn-dorpakke", "box"),
                    new Lenke("Serviceavtaler", "Avtaler, servicerunder og årshjul.", "service", "clock", null, ModulTilgang.FraProsjektleder),
                    new Lenke("Tilbud", "Gi pris på mindre jobber og service.", "tilbud-service", "receipt", null, ModulTilgang.FraProsjektleder),
                    new Lenke("CE-godkjenninger", "CE-dokumentasjon for automatiske dører.", "ce-godkjenninger", "shield", KreverModul: TilleggsModuler.Ce),
                    new Lenke("Prosjektøkonomi", "Økonomien i pågående prosjekter, og etterkalkyle når de er overlevert.", "prosjektokonomi", "trend-up", null, ModulTilgang.FraProsjektleder, KreverModul: TilleggsModuler.Okonomi),
                    // Begge ligger i venstremenyen og vises derfor ikke som
                    // egne kort i kategorien.
                    new Lenke("Timeregistrering", "Registrer timer på en arbeidsordre.", "timeregistrering", "clock", null, ModulTilgang.Alle, KunIMeny: true, KreverModul: TilleggsModuler.Timer),
                    new Lenke("Fravær", "Søk om ferie eller meld fravær.", "fravar", "sun", null, ModulTilgang.Alle, KunIMeny: true, KreverModul: TilleggsModuler.Timer),
                ]),
            ]),

        new Rolle(
            "montorverktoy",
            "Montørverktøy",
            "Befaring, verktøy og oversikt",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Min dag", "Dagens jobber", "min-dag", "calendar"),
                    new Lenke("Befaringsliste", "Notater fra befaring", "befaringsliste", "eye"),
                    new Lenke("Utskiftning av lås", "Sjekkliste for utskiftning", "lasutskifting", "tool"),
                    new Lenke("Utskiftning av låskasse", "Søk opp gammel lås, se hva du trenger.", "lasekasse-utskiftning", "tool"),
                    new Lenke("ABC-kalkulator", "Finn riktige skilt og skruelengder", "abc-kalkulator", "tool"),
                    new Lenke("Min bil", "Varer i bilen, bruk og retur", "min-bil", "map-pin", null, ModulTilgang.Alle, KreverModul: TilleggsModuler.Lager),
                ]),
            ]),

        new Rolle(
            "teknisk",
            "Teknisk og ADK",
            "Kobling, dørmiljø og oppsett av adgangskontroll",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Kobling", "Koblingsskjema", "guide/kobling", "trend-up"),
                    new Lenke("Dørmiljø", "Prinsippskisser av dørmiljø", "dormiljo", "box"),
                    new Lenke("Oppsett adgangskontroll", "Guide for oppsett av adgangskontroll.", "guide/oppsett-adgangskontroll", "bell"),
                    new Lenke("Systemregister", "Søk opp låssystemer", "systemregister", "users"),
                ]),
            ]),

        new Rolle(
            "personell",
            "Ressurser og godkjenninger",
            "Kalender og godkjenning av timer og fravær",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Kalender", "Ressursplanlegging og oversikt", "kalender", "calendar", KreverModul: TilleggsModuler.Timer),
                    new Lenke("Godkjenninger", "Godkjenning av timer og søknader", "timegodkjenning", "check-circle", null, ModulTilgang.FraProsjektleder, KreverModul: TilleggsModuler.Timer),
                ]),
            ]),

        new Rolle(
            "driftkunde",
            "Kunder og drift",
            "Kunderegister, tickets og forespørsler",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Kunderegister", "Opprett og vedlikehold kunder", "kunder", "users", null, ModulTilgang.FraProsjektleder),
                    new Lenke("Tickets", "Samlet kontroll og oversikt over henvendelser", "tickets", "tag", null, ModulTilgang.FraProsjektleder),
                    new Lenke("Forespørsler", "Mottatte forespørsler", "foresporsler", "mail", null, ModulTilgang.FraProsjektleder),
                ]),
            ]),

        // Tilleggsmodul - vises bare for organisasjoner som har kjøpt Lagerstyring,
        // og bare i venstremenyen (KunIMeny), ikke som kort under «Mine moduler».
        new Rolle(
            "lager",
            "Lager og innkjøp",
            "Beholdning, lagre, bevegelser og varetelling",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Lager", "Beholdning, innkjøp, varemottak og varetelling", "lager", "box", null, ModulTilgang.FraProsjektleder, KunIMeny: true, KreverModul: TilleggsModuler.Lager),
                ]),
            ]),

        // Tilleggsmodul - salg i butikk. Åpnes i fullskjerm (KassaLayout).
        new Rolle(
            "kassa",
            "Kassa",
            "Salg, kontantfaktura og oppgjør",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Kassa", "Salg med kort eller kontant, kontantfaktura, dagsoppgjør og bankavstemming", "kassa", "receipt", null, ModulTilgang.Alle, KunIMeny: true, KreverModul: TilleggsModuler.Kassa),
                ]),
            ]),

        new Rolle(
            "administrasjon",
            "Administrasjon",
            "Oppsett, komponenter, brukere og rapporter",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Oppsett og maler", "Dørpakker, krav, sjekklister, tilvalg og dokumenter samlet på ett sted", "oppsettmodul", "settings", OppsettUndermoduler, ModulTilgang.KunAdmin),
                    new Lenke("Komponenter og priser", "Komponentregister, prisimport, leverandører, rabattgrupper og produktgrupper samlet på ett sted", "komponentprismodul", "box", KomponentPrisUndermoduler, ModulTilgang.KunAdmin),
                    new Lenke("Brukere og kunder", "Brukere, kundetilgang og driftslisenser samlet på ett sted", "brukerkundemodul", "users", BrukerKundeUndermoduler, ModulTilgang.KunAdmin),
                    new Lenke("Rapporter", "Kunde-, produkt- og resultatrapport beregnet fra egne arbeidsordre, pluss kontoplan", "rapportermodul", "receipt", RapporterUndermoduler, ModulTilgang.KunAdmin),
                ]),
            ]),
    ];

    private static ModulTilgang TilgangFor(BrukerRolle? rolle) => rolle switch
    {
        BrukerRolle.Admin => ModulTilgang.Admin,
        BrukerRolle.Prosjektleder => ModulTilgang.Prosjektleder,
        BrukerRolle.Montor => ModulTilgang.Montor,
        _ => ModulTilgang.Ingen
    };

    // Alle kategoriene filtrert på hva rollen har tilgang til - KunIMeny er
    // ikke tatt hensyn til her. Grunnlaget for både menyen og forsidekortene.
    // aktiveModuler: tilleggsmodulene organisasjonen har (TilleggsmodulService).
    // Uten listen skjules alle lenker som krever en tilleggsmodul.
    private static List<Rolle> TilgangsfiltrerteKategorier(BrukerRolle? rolle, IReadOnlySet<string>? aktiveModuler = null)
    {
        var tilgang = TilgangFor(rolle);

        return Kategorier
            .Select(k => k with
            {
                Grupper = k.Grupper
                    .Select(g => g with
                    {
                        Lenker = g.Lenker
                            .Where(l => l.Tilgang.HasFlag(tilgang))
                            .Where(l => l.KreverModul is null || (aktiveModuler?.Contains(l.KreverModul) ?? false))
                            .Select(l => l.Undermoduler is null ? l : l with { Undermoduler = l.Undermoduler.Where(u => u.KreverModul is null || (aktiveModuler?.Contains(u.KreverModul) ?? false)).ToList() })
                            .ToList()
                    })
                    .Where(g => g.Lenker.Count > 0)
                    .ToList()
            })
            .Where(k => k.Grupper.Count > 0)
            .ToList();
    }

    // Kategoriene slik de vises på forsiden: bare moduler rollen har tilgang
    // til, og uten de som er merket KunIMeny. En kategori uten synlige moduler
    // faller bort av seg selv.
    public static List<Rolle> KategorierFor(BrukerRolle? rolle, IReadOnlySet<string>? aktiveModuler = null) => TilgangsfiltrerteKategorier(rolle, aktiveModuler)
        .Select(k => k with
        {
            Grupper = k.Grupper
                .Select(g => g with { Lenker = g.Lenker.Where(l => !l.KunIMeny).ToList() })
                .Where(g => g.Lenker.Count > 0)
                .ToList()
        })
        .Where(k => k.Grupper.Count > 0)
        .ToList();

    // Flat liste over modulene brukeren ser - til venstremenyen. Tar med
    // KunIMeny-modulene, som er hele poenget med dem.
    public static List<Lenke> ModulerFor(BrukerRolle? rolle, IReadOnlySet<string>? aktiveModuler = null) => TilgangsfiltrerteKategorier(rolle, aktiveModuler)
        .SelectMany(k => k.Grupper)
        .SelectMany(g => g.Lenker)
        .DistinctBy(l => l.Href)
        .ToList();

    // Som ModulerFor, men hub-modulene er foldet ut til undermodulene sine -
    // altså bare sider man faktisk kan lande på. Brukes av "Mine hurtigvalg"
    // på forsiden, som skal peke rett på en modul og ikke på en hub-nøkkel
    // ("oppsettmodul") det ikke finnes noen side for.
    public static List<Lenke> BladLenkerFor(BrukerRolle? rolle, IReadOnlySet<string>? aktiveModuler = null) => ModulerFor(rolle, aktiveModuler)
        .SelectMany(l => l.Undermoduler is { Count: > 0 } under ? under : [l])
        .DistinctBy(l => l.Href)
        .ToList();

    // Finner "ett steg tilbake"-målet for en gitt sluttside: hub-siden (med
    // eget navn/href) som lenker til denne siden i en av rollenes moduler.
    // Brukes av MainLayout til å vise "Til <hub>" i stedet for et generisk
    // "Til hjem" på sider som er en direkte undermodul av en hub (f.eks.
    // Befaringsliste -> Befaring). Returnerer null hvis siden ikke er en
    // kjent undermodul av noen hub (da faller MainLayout tilbake til "Til hjem").
    public static (string Href, string Tittel)? FinnForelderHub(string path)
    {
        var normalisert = path.Trim('/').ToLowerInvariant();
        foreach (var rolle in Roller)
        {
            foreach (var gruppe in rolle.Grupper)
            {
                foreach (var hub in gruppe.Lenker)
                {
                    if (hub.Undermoduler is null)
                    {
                        continue;
                    }

                    foreach (var under in hub.Undermoduler)
                    {
                        if (under.Href.Trim('/').Equals(normalisert, StringComparison.OrdinalIgnoreCase))
                        {
                            return (hub.Href, hub.Tittel);
                        }
                    }
                }
            }
        }

        return null;
    }
}
