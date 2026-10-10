namespace PortalItlock.Web.Models;

// Fargen på en aktivitetsblokk i kalenderen og på Min dag: en dempet flate,
// en kraftigere kantfarge til venstre og en mørk blekkfarge som er lesbar
// mot flaten.
public sealed record Fargepar(string Flate, string Kant, string Blekk);

// Felles palett for alle tidslinjer i portalen (Kalender, Min dag,
// Timeregistrering), slik at samme jobb får samme farge uansett hvor den
// vises. Fargene følger referansedesignet.
public static class AktivitetFarge
{
    public static readonly Fargepar Sand = new("#f6e7d7", "#d99a52", "#6b4523");
    public static readonly Fargepar Bla = new("#dce9f8", "#5b8fd0", "#27476e");
    public static readonly Fargepar Lilla = new("#e8e0f7", "#8b74c4", "#453369");
    public static readonly Fargepar Gronn = new("#ddeedd", "#5fa368", "#2c5231");
    public static readonly Fargepar Fersken = new("#fbe3d2", "#cc8352", "#73411f");
    public static readonly Fargepar Turkis = new("#d8ecec", "#4f9a9a", "#1f4f4f");

    // Rød brukes til fravær/sykemelding, grå til lunsj og interne poster -
    // de skal aldri blandes med de vanlige jobbfargene.
    public static readonly Fargepar Rod = new("#fadede", "#d06565", "#7a2e2e");
    public static readonly Fargepar Noytral = new("#eae6dd", "#a89b85", "#4c4636");

    private static readonly Fargepar[] Rullering = [Sand, Bla, Lilla, Gronn, Fersken, Turkis];

    // Samme nøkkel gir alltid samme farge. Nøkkelen er typisk prosjekt-id
    // eller arbeidsordre-id.
    public static Fargepar Velg(int nokkel) => Rullering[Math.Abs(nokkel) % Rullering.Length];
}
