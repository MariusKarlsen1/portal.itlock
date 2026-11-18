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
