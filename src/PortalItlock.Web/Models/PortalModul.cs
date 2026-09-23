namespace PortalItlock.Web.Models;

public sealed record Lenke(string Tittel, string Beskrivelse, string Href, string IconNavn = "folder", List<Lenke>? Undermoduler = null);
public sealed record LenkeGruppe(string Navn, List<Lenke> Lenker);
public sealed record Rolle(string Nokkel, string Tittel, string Beskrivelse, List<LenkeGruppe> Grupper);

public static class PortalModulRegister
{
    private static readonly List<Lenke> BefaringUndermoduler =
    [
        new Lenke("Befaringsliste", "Sjekkliste og notater fra befaring.", "befaringsliste", "check-circle"),
        new Lenke("Utskiftning av lås", "Sjekkliste med mål på dør, karm og hengsling.", "lasutskifting", "tool"),
    ];

    private static readonly List<Lenke> GuiderUndermoduler =
    [
        new Lenke("Kobling", "Guide for kobling.", "guide/kobling", "trend-up"),
        new Lenke("Dørmiljø", "Tegninger til bruk på befaring og i prosjektering.", "dormiljo", "box"),
        new Lenke("Oppsett adgangskontroll", "Guide for oppsett av adgangskontroll.", "guide/oppsett-adgangskontroll", "bell"),
    ];

    private static readonly List<Lenke> ProsjektMontorUndermoduler =
    [
        new Lenke("Prosjekter", "Prosjekter du er lagt til på.", "prosjekter", "folder"),
        new Lenke("Arbeidsordre", "Se arbeidsordre med info om jobben og ansvarlig montør.", "arbeidsordre", "receipt"),
        new Lenke("CE-godkjenninger", "CE-sertifiseringer for dører med automatikk, på tvers av prosjekter.", "ce-godkjenninger", "check-circle"),
    ];

    private static readonly List<Lenke> TimerFravarUndermoduler =
    [
        new Lenke("Fravær", "Søk om ferie eller meld fravær.", "fravar", "sun"),
        new Lenke("Timeregistrering", "Registrer timer på en arbeidsordre.", "timeregistrering", "clock"),
    ];

    private static readonly List<Lenke> ProsjekteringUndermoduler =
    [
        new Lenke("Prosjekter", "Dører, plantegninger og montasjestatus per prosjekt.", "prosjekter", "folder"),
        new Lenke("Arbeidsordre", "Opprett arbeidsordre med info om jobben og ansvarlig montør.", "arbeidsordre", "check-circle"),
        new Lenke("Finn dørpakke", "Huk av krav og finn riktig dørpakke til prosjektet.", "finn-dorpakke", "box"),
        new Lenke("Prisoverslag", "Rask kalkulator med komponenter, timer og påslag.", "prisoverslag", "receipt"),
        new Lenke("Systemregister", "Søk opp låssystem, kunde, adresse og rekvirenter.", "systemregister", "users"),
        new Lenke("CE Modul", "Alle CE-sertifiseringer på tvers av prosjekter, med status, vilkår og gyldighet.", "ce-godkjenninger", "shield"),
    ];

    private static readonly List<Lenke> KunderUndermoduler =
    [
        new Lenke("Kunderegister", "Opprett og vedlikehold kunder med kontaktinfo og adresse.", "kunder", "users"),
        new Lenke("Kundeoppfølging", "Kunder som bør følges opp, med logg og påminnelser.", "kundeoppfolging", "clock"),
        new Lenke("Tilvalg", "Publiser tilvalg til kunder, og se svar med valg og signatur.", "tilvalg", "tag"),
        new Lenke("Tilvalgsmaler", "Ferdige maler med alternativer, bilder og priser til gjenbruk.", "tilvalgmaler", "tag"),
    ];

    private static readonly List<Lenke> DriftUndermoduler =
    [
        new Lenke("Kart", "Se montørenes jobber geografisk for en valgt dag.", "kart", "map"),
        new Lenke("Kalender", "Se planlagte arbeidsordre for alle montører i månedsvisning.", "kalender", "calendar"),
        new Lenke("Fravær", "Søknader, godkjenning og fraværskalender per person.", "fravar", "sun"),
        new Lenke("Forespørsler", "E-poster sendt til post@itlock.no.", "foresporsler", "mail"),
    ];

    private static readonly List<Lenke> ServiceUndermoduler =
    [
        new Lenke("Serviceavtaler", "Serviceavtaler og serviceoppdrag samlet, med servicehistorikk og rapporter.", "service", "clock"),
        new Lenke("Tilbud service", "Opprett og se tilbud for serviceavtaler, med estimater fra arbeidsordre-minuttene.", "tilbud-service", "receipt"),
    ];

    private static readonly List<Lenke> OppsettUndermoduler =
    [
        new Lenke("Dørpakker", "Sett sammen dørpakker, eller rediger og slett eksisterende.", "dorpakker", "box"),
        new Lenke("Kravdimensjoner", "Vedlikehold kravkategoriene kravvelgeren bygger på.", "kravdimensjoner", "check-circle"),
        new Lenke("Dørfunksjoner", "Opprett og vedlikehold dørfunksjoner som kan legges på dører.", "dorfunksjoner", "tool"),
        new Lenke("Sjekklistemaler", "Digitale sjekklister per dørtype til bruk på arbeidsordre.", "sjekklistemaler", "alert"),
        new Lenke("Sjekkliste for servicerunder", "Punktene som automatisk legges til hver servicerunde.", "service-sjekklistemal", "clock"),
        new Lenke("Tilvalg", "Publiserte og besvarte tilvalg på tvers av prosjekter.", "tilvalg", "tag"),
        new Lenke("Tilvalgsmaler", "Ferdige maler med alternativer, bilder og priser til gjenbruk.", "tilvalgmaler", "receipt"),
        new Lenke("Forsider", "Lag forside-/forbeholdstekster til bruk på tilbud.", "forsider", "folder"),
        new Lenke("Timeoversikt", "Sammendrag og eksport av timer til lønn.", "timeoversikt", "calendar"),
        new Lenke("Hjemmesidetekster", "Rediger overskrift, ingress og rolle-tekstene på forsiden.", "hjemmesidetekster", "users"),
        new Lenke("CE-sertifisering", "Sett CE-kategori og standarddokument på beslagstyper, og grenseverdier for mål.", "ce-oppsett", "check-circle"),
        new Lenke("Nyheter", "Se automatisk genererte \"Hva er nytt\"-oppdateringer som vises på forsiden til alle brukere.", "nyheter", "bell"),
    ];

    private static readonly List<Lenke> KomponentPrisUndermoduler =
    [
        new Lenke("Komponentregister", "Søk, prissett og administrer komponenter og leverandører.", "komponenter", "tool"),
        new Lenke("Prisimport", "Last opp prisliste fra leverandør, oppdater priser og legg til nye varer.", "komponenter/prisimport", "receipt"),
        new Lenke("Leverandører", "Søk opp leverandører, se hvilke varer de leverer, og sett varenummer/pris/standard pr. leverandør.", "komponenter/leverandorer", "users"),
        new Lenke("Rabattgrupper", "Faste rabattavtaler per leverandør/produktserie, brukes til å beregne nettopris automatisk.", "rabattgrupper", "tag"),
        new Lenke("Produktgrupper", "Opprett produktgrupper og legg til varer i dem. Brukes til å sette kunderabatt per gruppe.", "produktgrupper", "folder"),
        new Lenke("Lagerstyring", "Hold oversikt over lagerbeholdning og få varsel ved lav beholdning.", "lager", "box"),
    ];

    private static readonly List<Lenke> BrukerKundeUndermoduler =
    [
        new Lenke("Brukere", "Opprett brukere med e-post, stilling og tilgang til portalen.", "brukere", "users"),
        new Lenke("Kunder", "Opprett og vedlikehold kunderegisteret.", "kunder", "folder"),
        new Lenke("Kundeoppfølging", "Kunder som bør følges opp, med logg og påminnelser.", "kundeoppfolging", "clock"),
        new Lenke("Kundetilganger", "Administrer innlogging til kundeportalen.", "kundetilganger", "bell"),
        new Lenke("Fravær", "Søknader, godkjenning og fraværskalender per person.", "fravar", "sun"),
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
                    new Lenke("Befaring", "Befaringsliste og utskiftning av lås", "befaringsmodul", "eye", BefaringUndermoduler),
                    new Lenke("Guider", "Kobling, dørmiljø og oppsett adgangskontroll", "guidermodul", "tool", GuiderUndermoduler),
                    new Lenke("Prosjekt", "Prosjekter og arbeidsordre", "prosjektmodul", "folder", ProsjektMontorUndermoduler),
                    new Lenke("Timer og fravær", "Fravær og timeregistrering", "timerfravarmodul", "clock", TimerFravarUndermoduler),
                    new Lenke("Utskiftning av låskasse", "Søk opp gammel lås, se hva du trenger", "lasekasse-utskiftning", "tool"),
                    new Lenke("ABC-kalkulator", "Sylinderforlenger, skilt og skruer fra A/B/C-mål", "abc-kalkulator", "tool"),
                ]),
                new LenkeGruppe("Kart og kalender", [
                    new Lenke("Min dag", "Dagens jobber, adresse og varer å pakke, samlet", "min-dag", "calendar"),
                    new Lenke("Kart", "Se dagens jobber geografisk", "kart", "map"),
                    new Lenke("Kalender", "Se planlagte arbeidsordre i månedsvisning", "kalender", "calendar"),
                ]),
            ]),
        new Rolle(
            "prosjektledere",
            "For prosjektledere",
            "Systemer, prosjektering og prosjekter",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Prosjekt modul", "Prosjekter, dørpakker, prisoverslag og systemregister samlet på ett sted", "prosjekteringmodul", "folder", ProsjekteringUndermoduler),
                    new Lenke("Kunder", "Kunderegister, oppfølging, portaltilgang og tilvalg samlet på ett sted", "kundemodul", "users", KunderUndermoduler),
                    new Lenke("Drift", "Kart, kalender og fravær samlet på ett sted", "driftmodul", "map", DriftUndermoduler),
                    new Lenke("Ressursplanlegger", "Sett av montører på dager fremover, vises i deres kalender", "ressursplanlegger", "calendar"),
                    new Lenke("Forespørsler", "E-poster sendt til post@itlock.no", "foresporsler", "mail"),
                    new Lenke("Tickets", "Samlet oversikt over alle henvendelser og saker", "tickets", "tag"),
                    new Lenke("Service modul", "Serviceavtaler, serviceoppdrag og tilbud service samlet på ett sted", "servicemodul", "clock", ServiceUndermoduler),
                ]),
            ]),
        new Rolle(
            "admin",
            "For admin",
            "Opprett og vedlikehold grunnlagsdata",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Oppsett og maler", "Dørpakker, krav, sjekklister, tilvalg og dokumenter samlet på ett sted", "oppsettmodul", "settings", OppsettUndermoduler),
                    new Lenke("Komponenter og priser", "Komponentregister, prisimport, rabattgrupper og lagerstyring samlet på ett sted", "komponentprismodul", "box", KomponentPrisUndermoduler),
                    new Lenke("Brukere og kunder", "Brukere, kunder, kundeoppfølging og fravær samlet på ett sted", "brukerkundemodul", "users", BrukerKundeUndermoduler),
                    new Lenke("Tripletex", "Test tilkobling og kundeoppslag mot Tripletex-API-et (testmiljø)", "integrasjoner/tripletex", "box"),
                    new Lenke("Rapporter", "Kunde-, produkt- og resultatrapport beregnet fra egne arbeidsordre, pluss kontoplan", "rapportermodul", "receipt", RapporterUndermoduler),
                ]),
            ]),
    ];

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
