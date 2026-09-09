namespace PortalItlock.Web.Services;

// Stabil, automatisk fargetildeling per BrukerId - brukes for å skille ansatte visuelt i
// Ressursplanlegger og Kalender. Fargen rangeres etter BrukerId i stigende rekkefølge (ikke
// alfabetisk), slik at en eksisterende ansatt alltid beholder samme farge når en ny ansatt
// legges til (nye Id-er er alltid høyest og havner bakerst i rangeringen), og alle ansatte får
// hver sin unike farge så lenge antallet ikke overstiger paletten.
public static class AnsattFarge
{
    private static readonly string[] Palett =
    [
        "#2f6fb3", // blå
        "#2b7a4b", // grønn
        "#c99a1e", // gul
        "#b23b3b", // rød
        "#6a4fae", // lilla
        "#1f8a8a", // turkis
        "#c2691e", // oransje
        "#a3527a", // rosa
        "#3f6b1f", // oliven
        "#2f4f8f", // marineblå
        "#8f4f2f", // brun
        "#5f8f8f", // dus turkis
    ];

    public static Dictionary<int, string> Bygg(IEnumerable<int> aktiveBrukerIder)
    {
        return aktiveBrukerIder
            .Distinct()
            .OrderBy(id => id)
            .Select((id, index) => (id, farge: Palett[index % Palett.Length]))
            .ToDictionary(x => x.id, x => x.farge);
    }
}
