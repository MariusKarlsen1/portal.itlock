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
