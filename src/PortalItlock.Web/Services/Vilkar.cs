namespace PortalItlock.Web.Services;

// Standardtekstene for Full Kontroll. De kopieres inn i PlattformInnstillinger
// første gang, og kan deretter redigeres i adminportalen (Innstillinger).
// Format: linjer som starter med «## » er overskrifter, «- » er punkter,
// tom linje skiller avsnitt. {Leverandor} byttes ut med leverandørens navn.
public static class Vilkar
{
    public const string StandardTilbudIntro =
        """
        Full Kontroll samler hele jobben i ett system: fra befaring og tilbud, via prosjekt, montering og arbeidsordre, til overlevering, FDV og serviceavtaler. Montørene jobber på mobilen, prosjektlederne ser fremdrift og økonomi mens prosjektet pågår, og kundene får sin egen portal.

        Under ser dere hva som er med, lisensene dere har valgt, og hva det koster per måned og per år.
        """;

    public const string StandardVilkar =
        """
        ## 1. Hvem avtalen gjelder
        Avtalen gjelder mellom {Leverandor} («Leverandøren») og virksomheten som bestiller eller bruker Full Kontroll («Kunden»). Den gjelder fra Kunden bestiller, signerer eller tar tjenesten i bruk, og går foran Kundens egne innkjøpsvilkår med mindre annet er avtalt skriftlig.

        ## 2. Tjenesten
        Full Kontroll leveres som en skytjeneste. Kunden får tilgang via nettleser og mobil, og ingenting installeres lokalt. Leverandøren utvikler tjenesten løpende og kan endre, forbedre eller legge til funksjoner. Endringer som tar bort vesentlig funksjonalitet Kunden betaler for, varsles minst 30 dager før.

        ## 3. Lisenser
        Kunden betaler for lisensene og modulene som er avtalt, og kan bruke tjenesten for egen virksomhet. Hver lisens gjelder én navngitt bruker og kan ikke deles. Lisenser for driftsansvarlige og Kundens kunder gir tilgang bare til det Kunden gir dem tilgang til.
        Kunden har ikke lov til å videreselge, leie ut eller gi andre tilgang til tjenesten, kopiere eller forsøke å finne kildekoden, eller bruke tjenesten på en måte som kan skade drift eller sikkerhet.

        ## 4. Kundens ansvar
        Kunden er ansvarlig for egne brukere og tilganger, for at passord holdes hemmelige, og for innholdet Kunden legger inn. Kunden skal selv sjekke dokumenter tjenesten lager, som tilbud, FDV, samsvarserklæringer og CE-dokumentasjon, før de brukes. Tjenesten er et verktøy og erstatter ikke faglige vurderinger eller krav i lov og forskrift.

        ## 5. Drift og tilgjengelighet
        Leverandøren arbeider for stabil drift og høy tilgjengelighet, men garanterer ikke oppetid. Planlagt vedlikehold gjøres så langt det går utenfor vanlig arbeidstid. Feil hos underleverandører, i Kundens eget utstyr eller nett, og forhold utenfor Leverandørens kontroll, regnes ikke som brudd på avtalen.

        ## 6. Sikkerhet og backup
        Data lagres kryptert og i EU/EØS. Hver kundes data ligger adskilt fra andre kunders. Det tas backup hver natt, kopiene kontrolleres og krypteres, og beholdes i inntil ett år. Backup reduserer risikoen for tap, men er ingen garanti for at alle data alltid kan gjenopprettes.

        ## 7. Personopplysninger
        Leverandøren behandler personopplysninger på vegne av Kunden etter databehandleravtalen, som er en del av denne avtalen.

        ## 8. Priser og prisregulering
        Prisene er i norske kroner uten mva. Prisene kan reguleres hvert år per 1. januar etter konsumprisindeksen. Andre prisendringer varsles minst 30 dager før de gjelder, og gjelder fra neste fakturaperiode.

        ## 9. Fakturering og betaling
        Abonnementet faktureres forskuddsvis per måned, eller per kvartal eller år etter avtale. Nye lisenser faktureres fra de blir aktivert, forholdsmessig for resten av perioden. Lisenser som tas bort, faktureres ut perioden som er fakturert. Betalingsfrist er 14 dager. Ved for sen betaling kan det kreves forsinkelsesrente og purregebyr.

        ## 10. Pause ved manglende betaling
        Er en faktura ikke betalt 30 dager etter forfall, kan Leverandøren sette tjenesten på pause etter skriftlig varsel. Ingen data slettes under pause, og tilgangen åpnes igjen når det utestående er betalt. Pause fritar ikke for betaling.

        ## 11. Oppsigelse
        Avtalen løper til den sies opp skriftlig. Oppsigelsestiden er én måned, regnet fra utgangen av måneden oppsigelsen kommer fram. Er det avtalt binding, gjelder den. Allerede fakturerte perioder refunderes ikke.

        ## 12. Kundens data når avtalen slutter
        Kunden kan laste ned en full kopi av sine data i 30 dager etter at avtalen er slutt. Deretter slettes dataene, også fra backup innen backupens lagringstid.

        ## 13. Ansvar
        Leverandøren er ikke ansvarlig for indirekte tap, som tapt fortjeneste eller tap av data som følge av Kundens egne handlinger. Leverandørens samlede ansvar er begrenset til det Kunden har betalt de siste tre månedene. Begrensningene gjelder ikke ved grov uaktsomhet eller forsett.

        ## 14. Rettigheter
        Alle rettigheter til Full Kontroll tilhører Leverandøren. Kunden eier sine egne data. Leverandøren kan oppgi Kunden som referanse, med mindre Kunden sier fra skriftlig.

        ## 15. Endringer i vilkårene
        Leverandøren kan oppdatere vilkårene. Vesentlige endringer varsles minst 30 dager før de gjelder, og Kunden kan da si opp avtalen før endringen trer i kraft.

        ## 16. Tvister
        Partene skal først prøve å løse uenighet ved forhandling. Avtalen følger norsk rett, og tvister avgjøres ved Leverandørens hjemting.
        """;

    public const string StandardDatabehandleravtale =
        """
        ## Partene
        Behandlingsansvarlig: Kunden.
        Databehandler: {Leverandor}.
        Avtalen gjelder personopplysninger som behandles når Kunden bruker Full Kontroll, og er en del av lisens- og tjenestevilkårene.

        ## 1. Formål og instruks
        Databehandler behandler personopplysninger bare for å levere Full Kontroll til Kunden, og bare etter Kundens instruks. Instruksen er denne avtalen og hvordan Kunden selv bruker tjenesten.

        ## 2. Hvilke opplysninger og hvem det gjelder
        Opplysningene kan være navn, kontaktinformasjon, innloggingsinformasjon, timer og fravær, bilder og dokumentasjon fra jobber, og annet Kunden legger inn.
        - Kundens ansatte og innleide
        - Kundens kunder, driftsansvarlige og samarbeidspartnere
        - Andre personer Kunden registrerer i tjenesten

        ## 3. Databehandlers plikter
        - Behandle opplysningene i tråd med personvernforordningen (GDPR) og norsk personvernlov
        - Sørge for at alle med tilgang har taushetsplikt
        - Ha tekniske og organisatoriske sikkerhetstiltak tilpasset risikoen
        - Hjelpe Kunden med innsyn, retting, sletting og andre rettigheter de registrerte har
        - Hjelpe Kunden med konsekvensvurderinger når det er relevant

        ## 4. Sikkerhet
        Data lagres kryptert, og hver kundes data ligger adskilt. Tilgang er begrenset til personer som trenger den for drift og støtte, og logges. Det tas kryptert backup hver natt.

        ## 5. Underleverandører
        Databehandler bruker underleverandører for drift, lagring og e-post. Kunden får oversikt over dem på forespørsel, og varsles før nye tas i bruk. Underleverandørene har tilsvarende plikter som databehandler.

        ## 6. Hvor dataene ligger
        Opplysningene behandles innenfor EU/EØS. Skulle det bli aktuelt å behandle data utenfor EU/EØS, skjer det bare i tråd med GDPR kapittel V.

        ## 7. Brudd på sikkerheten
        Databehandler varsler Kunden uten ugrunnet opphold ved brudd på sikkerheten, og gir Kunden informasjonen som trengs for å melde fra til Datatilsynet.

        ## 8. Revisjon
        Kunden kan be om dokumentasjon på sikkerhetstiltakene. Revisjoner avtales i god tid og gjennomføres uten unødig ulempe for driften.

        ## 9. Når avtalen slutter
        Når tjenesteavtalen slutter, kan Kunden laste ned sine data i 30 dager. Deretter slettes personopplysningene, også fra backup innen backupens lagringstid.
        """;
}
