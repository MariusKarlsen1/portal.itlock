namespace PortalItlock.Web.Models;

// Kategoriene i "Vis i kalender" på /kalender. De to første kommer av seg selv
// fra arbeidsordrene (med eller uten prosjekt); resten er hendelser som legges
// inn manuelt, og lagres i Ressursplan.Kategori.
public static class KalenderKategori
{
    public const string Arbeidsordre = "arbeidsordre";
    public const string Prosjekt = "prosjekt";
    public const string Mote = "mote";
    public const string Fravar = "fravar";
    public const string Service = "service";
    public const string Opplaering = "opplaering";
    public const string Internt = "internt";

    // Rekkefølgen i filterlista.
    public static readonly (string Nokkel, string Navn, string Farge)[] Alle =
    [
        (Arbeidsordre, "Arbeidsordre", "#2b7a4b"),
        (Prosjekt, "Prosjekter", "#2054a3"),
        (Mote, "Møter", "#7a2b96"),
        (Fravar, "Fravær", "#c0392b"),
        (Service, "Service", "#0c9c5c"),
        (Opplaering, "Opplæring", "#c2690a"),
        (Internt, "Interne oppgaver", "#6b6863")
    ];

    // Hendelsene man kan legge inn selv - arbeidsordre og prosjekt kommer fra
    // arbeidsordrene og skal ikke kunne velges i skjemaet.
    public static readonly (string Nokkel, string Navn)[] Hendelsestyper =
    [
        (Mote, "Møte"),
        (Fravar, "Fravær"),
        (Service, "Service"),
        (Opplaering, "Opplæring"),
        (Internt, "Internt")
    ];

    public static string Navn(string nokkel) =>
        Alle.FirstOrDefault(k => k.Nokkel == nokkel).Navn ?? "Internt";

    public static string Farge(string nokkel) =>
        Alle.FirstOrDefault(k => k.Nokkel == nokkel).Farge ?? "#6b6863";

    public static string Ikon(string nokkel) => nokkel switch
    {
        Mote => "users",
        Fravar => "sun",
        Service => "tool",
        Opplaering => "users",
        Prosjekt => "folder",
        Arbeidsordre => "check-circle",
        _ => "clipboard"
    };

    // Flate og blekk til blokkene i rutenettet - samme oppskrift som de andre
    // tintene i portalen: fargen blandet mot den hvite flaten.
    public static Fargepar Fargepar(string nokkel)
    {
        var kant = Farge(nokkel);
        return new Fargepar(
            $"color-mix(in srgb, {kant} 14%, #ffffff 86%)",
            kant,
            $"color-mix(in srgb, {kant} 78%, #292927 22%)");
    }
}
