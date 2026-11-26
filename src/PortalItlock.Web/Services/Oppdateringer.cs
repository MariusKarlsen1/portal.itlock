namespace PortalItlock.Web.Services;

// «Hva er nytt»: kort liste over endringer per versjon. Øverst er nyeste.
// Når en ny versjon legges inn her og pushes, ser hver bruker vinduet én gang
// (se Components/HvaErNytt.razor). Hold det kort: 3–6 punkter, én linje hver,
// vanlig språk. Ikon = navn i Components/Icon.razor.
public static class Oppdateringer
{
    public enum Type { Nytt, Bedre, Fikset }

    public sealed record Punkt(Type Type, string Ikon, string Tittel, string Tekst);

    public sealed record Versjon(string Nummer, DateOnly Dato, IReadOnlyList<Punkt> Punkter);

    public static readonly IReadOnlyList<Versjon> Alle =
    [
        new("1.36", new DateOnly(2026, 11, 26),
        [
            new(Type.Nytt, "box", "Varer og priskoder synkes med Tripletex", "Priskoder går begge veier. Varer opprettes i Tripletex første gang de brukes på en ordre, og varer laget i Tripletex hentes hit."),
            new(Type.Bedre, "users", "Kontaktperson og vår kontakt på ordren", "Ordren i Tripletex får kundens kontaktperson og den som laget tilbudet. Referansen er aldri arbeidsordrenummeret."),
            new(Type.Nytt, "edit", "Kommentar til kunde", "Skriv f.eks. bestillingsnummeret ved ferdigmelding. Det havner i «Kommentar til kunde» i Tripletex."),
            new(Type.Fikset, "tag", "Rabatt per varelinje", "Rabatt % sendes bare på linjer priset med rabatt på veiledende. De andre linjene får bare prisen."),
            new(Type.Fikset, "link-ikon", "Kundesynken stopper ikke på dubletter", "To kunder koblet til samme kunde i Tripletex stoppet synken. Nå fortsetter den og sier hvilke kunder det gjelder."),
        ]),
        new("1.35", new DateOnly(2026, 11, 26),
        [
            new(Type.Nytt, "receipt", "Velg hva som legges i Tripletex", "Ved ferdigmelding velger du alle varelinjer, eller iht. tilbud med bare summen."),
            new(Type.Bedre, "tag", "Rabatt bare når tilbudet har rabatt", "Er tilbudet priset med rabatt på veiledende, vises veiledende pris og rabatt %. Med DG, påslag eller veiledende vises bare prisen."),
            new(Type.Fikset, "link-ikon", "Tydeligere feil fra Tripletex", "Når synken feiler, vises Tripletex sitt eget svar."),
        ]),
        new("1.34", new DateOnly(2026, 11, 26),
        [
            new(Type.Bedre, "receipt", "Ny Fakturering-fane", "Klart, fakturert og gjenstår øverst, akontoen som er klar med én knapp, og Rediger for å endre faktureringsplanen."),
            new(Type.Nytt, "check", "Fakturert i Tripletex vises her", "Når du har fakturert ordren inne i Tripletex, settes arbeidsordren til Fakturert i portalen av seg selv. Betalte akontoer blir Betalt."),
            new(Type.Bedre, "link-ikon", "Enklere kobling til Tripletex", "Lim inn en API-nøkkel fra Selskap → API-nøkler i Tripletex. Over 1000 kunder og varer hentes nå også."),
            new(Type.Nytt, "file-text", "Velg mottaker på tilbuds-PDF", "Prosjektets kunde, en annen kunde, skriv inn selv eller ingen. Gyldig til og levering vises bare når de er fylt ut."),
            new(Type.Fikset, "edit", "Renere overskrifter i tilbud", "Overskrift og beskrivelse i varelinjene vises uten ramme."),
        ]),
        new("1.33", new DateOnly(2026, 11, 26),
        [
            new(Type.Bedre, "trend-up", "Ny Økonomi-fane", "Fortjenesten mot prognosen som store tall, ett råd, og arbeid, varer og kjøring som rader du kan åpne."),
            new(Type.Bedre, "calendar", "Fremdrift som tidslinje", "Uker, denne uken, milepæler og stolper som viser hva som er gjort. Kryss av oppgavene rett i planen."),
            new(Type.Nytt, "receipt", "Fakturer uten Tripletex", "Uten tilkoblet økonomisystem spør portalen om det er fakturert i deres eget system."),
            new(Type.Fikset, "file-text", "Overskrifter i tilbud", "Overskrift og beskrivelse går over hele raden igjen."),
            new(Type.Bedre, "list", "Modulene i menyen", "Kunder, Brukere og kunder og Oppsett åpner valgvinduet i stedet for den gamle siden."),
        ]),
        new("1.32", new DateOnly(2026, 11, 26),
        [
            new(Type.Bedre, "trend-up", "Enklere prosjektøkonomi", "Fire faner inne på prosjektet: Oversikt, Økonomi, Fakturering og Fremdrift, med svaret i én setning øverst."),
            new(Type.Bedre, "receipt", "Fakturering med én knapp", "Akontoen som er klar, sendes rett fra toppen av fanen."),
            new(Type.Bedre, "check", "Kryss av oppgaver", "Trykk på en aktivitet i Fremdrift og kryss av oppgavene."),
        ]),
        new("1.31", new DateOnly(2026, 11, 26),
        [
            new(Type.Bedre, "mail", "Forespørsler uten tickets", "Status, ansvarlig, prioritet, frist og kunde settes rett på forespørselen. Ticket-knappene er borte."),
            new(Type.Nytt, "tool", "Lag arbeidsordre eller tilbud", "Rett fra forespørselen, med kunde, adresse, tekst og vedlegg ferdig utfylt."),
            new(Type.Nytt, "send", "Videresend fra Outlook", "Videresendt e-post får kunden som avsender, og kobles til riktig kunde av seg selv."),
            new(Type.Nytt, "globe", "Nettskjema for kunder", "Slå det på under Forespørsler → Nettskjema, og del lenken eller legg den på nettsiden."),
            new(Type.Bedre, "trend-up", "Prosjektøkonomi inne på prosjektet", "Status i klartekst, nøkkeltall og veien fra tilbudt til fortjeneste, med fakturering og fremdrift ved siden av."),
            new(Type.Fikset, "users", "Kunngjøringer bare for ansatte", "Kunder og driftslisenser ser ikke kunngjøringene lenger."),
        ]),
        new("1.30", new DateOnly(2026, 11, 26),
        [
            new(Type.Fikset, "phone", "Rulling i montørappen på Android", "Du kan igjen rulle nedover med fingeren på Hjem, arbeidsordre og prosjekt i Chrome på Android."),
        ]),
        new("1.29", new DateOnly(2026, 11, 26),
        [
            new(Type.Nytt, "home", "Ny driftsportal", "Mine bygg, plantegning med zoom der du trykker på en dør for å se den, dørliste og meldinger på hvert bygg."),
            new(Type.Nytt, "lock", "Tofaktor i driftsportalen", "Driftsbrukere kan slå på tofaktor og bytte passord under Min konto, og du kan kreve det under Brukere."),
            new(Type.Bedre, "calendar", "Røde dager i kalenderen", "Helligdager vises i fraværskalenderen og ukevisningen, og befaringslisten har status i nedtrekk."),
            new(Type.Bedre, "check", "Ryddigere to-do", "Bare i dag og forfalt vises, kommende ligger lukket under, og utførte havner nederst."),
            new(Type.Bedre, "list", "Kunngjøringer med ikoner", "Strekikoner på innleggstyper, filter og tydelig varsel på viktige innlegg."),
        ]),
        new("1.28", new DateOnly(2026, 11, 25),
        [
            new(Type.Nytt, "receipt", "ARX-kalkulator", "Velg dører, leser, låser og sentral ett steg om gangen, og få hardware, batteri og lisenser med kost og bruttopris."),
            new(Type.Bedre, "receipt", "Ny StepLock-kalkulator", "Samme oppsett som ARX: ett spørsmål om gangen, med abonnement og priser til slutt."),
            new(Type.Nytt, "plus", "Kalkulator rett i tilbudet", "«Legg til fra kalkulator» øverst på tilbudet legger linjene inn under egen overskrift."),
            new(Type.Bedre, "calendar", "Enklere ressursplan", "Større og ryddigere, med periode i nedtrekk og kapasitet du kan redigere."),
            new(Type.Bedre, "alert-triangle", "Tydeligere brannkontroll", "Alle dører med C i brannklassen vises røde eller grønne etter om de har dørlukker eller automatikk."),
        ]),
        new("1.27", new DateOnly(2026, 11, 24),
        [
            new(Type.Nytt, "file-text", "Mal for ordrebekreftelse", "Under Oppsett og maler bestemmer du felt, tekster og betingelser på ordrebekreftelsen, og kan legge til egne punkter."),
            new(Type.Bedre, "list", "Ryddigere ordrebekreftelse", "Annenhver grå og hvit rad i varelinjene, og tomme felt tas bort."),
        ]),
        new("1.26", new DateOnly(2026, 11, 24),
        [
            new(Type.Bedre, "search", "Ordrelinjer som på tilbud", "Søk på vare, leverandør, produktkode eller priskode øverst, med fritekstlinje ved siden av."),
            new(Type.Bedre, "file-text", "Se ordrebekreftelsen med en gang", "Trykk på en ordrebekreftelse for å forhåndsvise PDF-en."),
        ]),
        new("1.25", new DateOnly(2026, 11, 24),
        [
            new(Type.Nytt, "receipt", "Arbeidsordre uten tilbud", "Velg «Direkte på ordren» ved ny arbeidsordre. Legg inn varer, pris og rabatt, og timer og kjøring kommer med av seg selv."),
            new(Type.Nytt, "file-text", "Generer ordrebekreftelse", "Lag ordrebekreftelse rett fra arbeidsordren når det ikke finnes noe tilbud."),
            new(Type.Bedre, "filter", "Ryddigere dørliste", "Visning og status i nedtrekk, og filtrering i et lite vindu ved knappen."),
            new(Type.Bedre, "box", "Bilder i dørpakker", "Hver komponent har bilde. Hold musa over for å se det stort."),
        ]),
        new("1.24", new DateOnly(2026, 11, 20),
        [
            new(Type.Nytt, "shield", "Tofaktor-innlogging", "Kode fra en gratis autentiseringsapp i tillegg til passordet. Slå på under brukermenyen."),
            new(Type.Nytt, "lock", "Reservekoder og husket enhet", "Åtte reservekoder hvis telefonen er borte, og du kan slippe koden på egen PC i 30 dager."),
            new(Type.Nytt, "users", "Admin kan kreve tofaktor", "Krev det per rolle under Brukere, og tilbakestill for den som har mistet telefonen."),
        ]),
        new("1.23", new DateOnly(2026, 11, 20),
        [
            new(Type.Bedre, "edit", "Tydelig markering i koblingsskjema", "Det du markerer blir blått, med ramme og antall. Ctrl + V limer inn der du trykker."),
            new(Type.Nytt, "box", "Nye bilder av komponenter", "ARX dørsentral og StepLock OSDP med alle klemmene."),
        ]),
        new("1.22", new DateOnly(2026, 11, 20),
        [
            new(Type.Fikset, "check-circle", "Kopier og lim inn i koblingsskjema", "Dra en boks oppå tegningen, også over bakgrunnsbildet. Ctrl + V virker uten å trykke Rediger først."),
            new(Type.Nytt, "edit", "Ctrl + A og Ctrl + dra", "Marker alt med Ctrl + A, eller dra en boks hvor som helst med Ctrl inne."),
        ]),
        new("1.21", new DateOnly(2026, 11, 20),
        [
            new(Type.Nytt, "maximize", "Marker i koblingsskjema", "Trykk Marker (M) og dra en boks hvor som helst. Kopier og lim inn på et annet skjema."),
            new(Type.Bedre, "door", "Tydeligere dørark på mobil", "Beslag fra dørprodusent og elektro har fargede LD- og LE-merker."),
        ]),
        new("1.20", new DateOnly(2026, 11, 20),
        [
            new(Type.Bedre, "list", "Ryddigere forside på mobil", "Snarveiene står samlet i én liste, så mer får plass uten å scrolle."),
        ]),
        new("1.19", new DateOnly(2026, 11, 20),
        [
            new(Type.Nytt, "door", "Dørsiden på mobil", "Info, beslag, historikk, bilder og dokumenter, og meld avvik rett fra døra."),
            new(Type.Fikset, "check-circle", "Blir i mobilvisningen", "«Åpne døra» og tilbake tar deg ikke lenger ut til PC-visningen."),
        ]),
        new("1.18", new DateOnly(2026, 11, 20),
        [
            new(Type.Fikset, "check-circle", "Serviceavtaler på mobil", "Siden ser riktig ut igjen."),
            new(Type.Bedre, "list", "Annenhver grå rad på mobil", "Også på ordre og prosjekter."),
            new(Type.Bedre, "search", "Jevnere zoom i koblingsskjema", "Hold mellomrom og dra for å flytte rundt, også når du redigerer."),
            new(Type.Bedre, "edit", "Dobbeltklikk for panelet", "Panelet for en komponent åpnes med dobbeltklikk, og kabler oppå komponenter kan trykkes på."),
            new(Type.Bedre, "box", "Nye ARX-tegninger", "ARX dørsentral og ARX Power X008 i samme stil som de andre."),
        ]),
        new("1.17", new DateOnly(2026, 11, 20),
        [
            new(Type.Nytt, "shield", "Serviceavtaler på mobil", "Tilbud fra +, Prising og Tilbud, og fanene Runder, Dører, Plan og Avvik."),
            new(Type.Nytt, "box", "Kopier mellom tegninger", "Marker komponenter og kabler i koblingsskjema, kopier og lim inn på en annen tegning."),
            new(Type.Nytt, "plus", "Nye ARX-symboler", "ARX dørsentral og ARX Power X008 med batterier, med alle klemmene."),
            new(Type.Bedre, "door", "Dører på serviceavtaler", "Står som montert."),
            new(Type.Fikset, "check-circle", "Åpne utførte runder", "Utførte og eldre runder åpnes i visningen med plantegning og dører."),
        ]),
        new("1.16", new DateOnly(2026, 11, 20),
        [
            new(Type.Bedre, "file-text", "Prosjekt til serviceavtale", "Får alltid det nye oppsettet med Prising og Tilbud. Tidligere runder følger med."),
            new(Type.Nytt, "sparkle", "Bytt til nytt oppsett", "Eldre serviceavtaler kan få de nye fanene og rundene med én knapp."),
            new(Type.Bedre, "list", "Annenhver grå rad på mobil", "Arbeidsordre, prosjekter, serviceavtaler og dører, som på PC."),
        ]),
        new("1.15",new DateOnly(2026, 11, 20),
        [
            new(Type.Nytt, "tool", "Enklere servicerunder", "Trykk på døren på plantegningen og velg Sjekket eller Avvik. Bilder og byttede deler havner på døren."),
            new(Type.Nytt, "door", "Serviceavtaler som prosjekter", "Dører, plantegninger, FDV, personer og vedlegg er de samme som på prosjektet, og avtalen har egen sjekkliste."),
            new(Type.Nytt, "file-text", "Tilbud på serviceavtale", "Forside som på vanlige tilbud, ferdig avtaletekst og tillegg fra + på Prising. Fra prosjekt telles dørene automatisk."),
            new(Type.Nytt, "map", "Kjøring på tilbud", "Antall kjøringer foreslås ut fra montasjetimene."),
            new(Type.Fikset, "check-circle", "Plantegning og tilbud", "Tegningen flytter seg ikke ved «Ny dør her», og forespørselen kommer med på tilbudet."),
        ]),
        new("1.14",new DateOnly(2026, 11, 20),
        [
            new(Type.Nytt, "file-text", "Tilbud serviceavtale", "Trykk + på antall per kategori, legg til tillegg og lag tilbudet som PDF."),
            new(Type.Nytt, "check-circle", "Tilbud godkjent", "Gjør tilbudet til en serviceavtale med dører, plantegninger og servicerunder."),
            new(Type.Bedre, "settings", "Enklere oppsett", "Oppsett for serviceavtaler har bare priser og tillegg."),
            new(Type.Nytt, "map", "Bytt tegning", "Velg en annen tegning i prosjektet rett fra tittelen på plantegningen."),
            new(Type.Fikset, "search", "Zoom i koblingsskjema", "Musehjulet zoomer uten å hoppe tilbake."),
        ]),
        new("1.13",new DateOnly(2026, 11, 19),
        [
            new(Type.Nytt, "file-text", "Tekst på hver leder", "Skriv fri tekst i begge ender av hver leder i en kabel, f.eks. «+12V» eller «NO»."),
            new(Type.Bedre, "edit", "Over lederen eller på enden", "Velg hvor teksten på lederne skal stå."),
        ]),
        new("1.12",new DateOnly(2026, 11, 19),
        [
            new(Type.Nytt, "list", "Velg dører på opsjonstilbud", "Filtrer på dørnummer, pakke og etasje, og huk av dørene du vil ha med."),
            new(Type.Nytt, "layers", "Sorter og grupper opsjonene", "F.eks. etasje og så pakke, med delsum per gruppe på PDF-en."),
            new(Type.Bedre, "edit", "Overskrift per dør", "Velg om dør-ID, dørnavn, etasje og pakke står på tilbudet."),
            new(Type.Fikset, "check-circle", "Feilmelding i koblingsskjema", "Kom av og til når man gikk ut av et skjema. Nå rettet."),
        ]),
        new("1.11", new DateOnly(2026, 11, 19),
        [
            new(Type.Nytt, "clipboard", "Beskjed til montøren", "Infokort på dørkortet som montøren ser på mobilen."),
            new(Type.Nytt, "box", "Pakker på dører", "Endrer du varer, velger du denne døren eller alle med pakken."),
            new(Type.Bedre, "list", "Fast rekkefølge", "Varer, opsjoner og dørfunksjoner står alltid likt."),
            new(Type.Bedre, "check-circle", "Unike dørnummer", "Varsel hvis dørnummeret finnes fra før i prosjektet."),
            new(Type.Bedre, "file-text", "Tilbud og lister", "Firmanavn på tilbuds-PDF, og annenhver grå rad i arkivlistene."),
        ]),
        new("1.10", new DateOnly(2026, 11, 19),
        [
            new(Type.Bedre, "link", "Hvit kabel", "Grå kant rundt, og sort merking i hvit boks."),
            new(Type.Nytt, "edit", "Kabel med ledere", "Velg hvor kabelnavnet står, og lengde på hver leder."),
            new(Type.Nytt, "plus", "Motorlås (låskasse)", "Nytt symbol under «Låser og sluttstykker»."),
        ]),
        new("1.9", new DateOnly(2026, 11, 19),
        [
            new(Type.Nytt, "link", "Kabler helt inn til punktet", "Velg på komponenten om kablene skal synes oppå den."),
            new(Type.Nytt, "file-text", "Merking på enden", "Merkingen ved kabelenden kan stå over streken eller på enden."),
            new(Type.Nytt, "edit", "Farge på merkingen", "Egen farge, så den synes mot komponenten."),
        ]),
        new("1.8", new DateOnly(2026, 11, 19),
        [
            new(Type.Fikset, "check-circle", "Feilmelding i koblingsskjema", "Kom når man gikk inn og ut av et skjema. Nå rettet."),
            new(Type.Nytt, "box", "Dupliser koblingsskjema", "Lag en kopi med alt innhold å bygge videre på."),
            new(Type.Bedre, "search", "Ryddig ved utzoom", "Tekst ved kabler og tekstbokser vises når du zoomer inn."),
            new(Type.Nytt, "plus", "StepLock OSDP", "Nytt symbol under «Lesere og sentraler»."),
        ]),
        new("1.7", new DateOnly(2026, 11, 19),
        [
            new(Type.Fikset, "check-circle", "Feilmelding i koblingsskjema", "Siden krasjer ikke lenger når du redigerer raskt."),
            new(Type.Bedre, "search", "Zoom så langt du vil", "Teksten krymper og skjules når du zoomer ut, så ingenting går over hverandre."),
            new(Type.Bedre, "file-text", "Bedre PDF og forhåndsvisning", "Tegningen fyller plassen og er ikke lenger klemt sammen."),
            new(Type.Bedre, "users", "Mobil for montører", "Koblingsskjemaet zoomer og skalerer som i portalen."),
        ]),
        new("1.6", new DateOnly(2026, 11, 19),
        [
            new(Type.Fikset, "link", "Små halvmåner og prikker", "Like små uansett zoom, og blir mindre når du zoomer ut."),
            new(Type.Nytt, "file-text", "Fet skrift og små tekstbokser", "Tekst og prikk kan gjøres så små du vil."),
            new(Type.Bedre, "edit", "Ryddigere redigering", "Firkanten for størrelse vises bare på det du har valgt."),
        ]),
        new("1.5", new DateOnly(2026, 11, 19),
        [
            new(Type.Fikset, "link", "Koblingsskjema ser riktig ut", "Små prikker og halvmåner. Rammen følger bildet, også på gamle skjema."),
            new(Type.Bedre, "edit", "Koblingspunkter legger du selv", "Ingen faste punkter på komponentene lenger."),
            new(Type.Nytt, "file-text", "Tekst ved strekenden", "F.eks. «+12V» eller «NO» rett over streken der den er koblet."),
            new(Type.Nytt, "plus", "Fri tekst og sort prikk", "Skriv hva du vil, endre størrelse og dra dit du vil."),
        ]),
        new("1.4", new DateOnly(2026, 11, 19),
        [
            new(Type.Bedre, "link", "Enklere koblingsskjema", "Rediger-knapp, navn rett over, kort panel. Mer ligger under «Mer …»."),
            new(Type.Nytt, "box", "Nye symboler", "Strømforsyning 24V, adapter 12V, relékort og klemmerekker."),
            new(Type.Nytt, "edit", "Raskere redigering", "Ctrl+C / Ctrl+V, Delete eller Backspace, og fri flytting."),
            new(Type.Bedre, "eye", "Ryddigere tegning", "Vanlige kabelfarger, skjul navn, og punkter vises bare ved musepekeren."),
            new(Type.Fikset, "check-circle", "Forhåndsvisning", "Skjemalista viser nå komponentene, ikke bare strekene."),
        ]),
        new("1.3", new DateOnly(2026, 11, 19),
        [
            new(Type.Nytt, "search", "Beslag på serviceavtaler", "Legg til og fjern rett i lista, med søk i komponentregisteret."),
            new(Type.Bedre, "list", "Arkivlistene", "Ferdigstilte, avslåtte, godkjente, fakturerte: lik vanlig liste, med år og sum."),
            new(Type.Bedre, "grid", "Dashbord", "Oversikten har samme tabeller som Arbeidsordre."),
            new(Type.Nytt, "users", "Montører og serviceavtaler", "Montører kan se, redigere og opprette serviceavtaler."),
            new(Type.Fikset, "shield", "Tilgang for montører", "Ingen kassa og ingen sletting. Prosjekt og dører er bare til visning."),
        ]),
        new("1.2", new DateOnly(2026, 11, 18),
        [
            new(Type.Nytt, "layers", "Ny revisjon av plantegning", "Egen knapp inne på tegningen. Dørene står der de sto."),
            new(Type.Nytt, "tag", "Fast timepris på kunden", "Settes på kundekortet og fylles inn på nye tilbud."),
            new(Type.Nytt, "sparkle", "Hva er nytt", "Etter hver oppdatering ser du de viktigste endringene her."),
            new(Type.Bedre, "zap", "Raskere portal", "Varebilder og dokumenter lagres smartere."),
        ]),
        new("1.1", new DateOnly(2026, 11, 18),
        [
            new(Type.Nytt, "scan", "Les dører fra PDF", "Dør ID, mål og høyre/venstre hentes fra plantegningen."),
            new(Type.Bedre, "users", "Flere montører på ordren", "Vises som rundinger i arbeidsordrelisten."),
            new(Type.Fikset, "check-circle", "Samme tegning på nytt", "Blir lastet opp som revisjon, ikke som ny tegning."),
        ]),
        new("1.0", new DateOnly(2026, 11, 18),
        [
            new(Type.Nytt, "list", "Overskrifter på tilbud", "Med delsum, felles meny og mulighet til å felle sammen."),
            new(Type.Nytt, "building", "Kunde på nytt system", "Velg kunde, så fylles adresse og kontaktperson inn."),
            new(Type.Bedre, "link", "Koblingsskjema", "Halvmåne der streker krysser, prikk der de er koblet."),
            new(Type.Bedre, "edit", "Ny arbeidsordre", "Åpnes rett på Rediger."),
            new(Type.Fikset, "check-circle", "Komponentregisteret", "Egendefinerte varer fra dører vises ikke lenger der."),
        ]),
    ];

    public static Versjon Siste => Alle[0];

    public static string Navn(Type t) => t switch
    {
        Type.Nytt => "Nytt",
        Type.Bedre => "Bedre",
        _ => "Fikset"
    };

    public static string Klasse(Type t) => t switch
    {
        Type.Nytt => "ny",
        Type.Bedre => "bedre",
        _ => "fiks"
    };
}
