namespace PortalItlock.Web.Models;

// Faste valglister til CE-godkjenning, delt mellom prosjekt-/dørskjemaene og CE-veiviseren.
public static class CeAlternativer
{
    public static readonly string[] Byggkategorier =
    [
        "Barnehage / barneskole",
        "Barnehage / barneskole personalområde",
        "Ungdomsskole / Vgs / universitet",
        "Offentlig - Publikumsområde",
        "Offentlig - Personalområde",
        "Kontor - Publikumsområde",
        "Kontor - Personalområde",
        "Publikumsområde",
        "Bolig",
        "Helse / omsorg - Pasientområde",
        "Helse / omsorg - Personalområde",
        "Eldrehjem / omsorgsbolig",
        "Annet - Høy risiko",
        "Annet - Tolerert risiko"
    ];

    public static readonly string[] Risikoklasser = ["Høy risiko", "Tolerert risiko"];

    public static readonly string[] EnergiKlasser = ["Lav", "Høy"];

    public static readonly int[] GyldighetMånederAlternativer = [3, 6, 9, 12];

    public static readonly string[] Konstruksjoner = ["Glass", "Aluminium", "Stål", "Tre"];

    public static readonly string[] DorbladAlternativer = ["Enkel", "Dobbel"];

    public static readonly string[] DekningsomradeAlternativer = ["Ingen dekning", "Full dørbredde", "I henhold til vedlegg G"];

    public static readonly string[] GlassIDorAlternativer = ["Med glass", "Uten glass"];

    public static readonly string[] TypeAvGlassAlternativer =
        ["Uten glass", "Herdet", "Laminert", "Sikkerhetsfilm", "Ubehandlet glass", "Trådglass", "Herdet/Laminert"];

    public static readonly string[] GlasstykkelseAlternativer =
    [
        "Aluminum/glass 8/24/8",
        "Aluminum/glass 12 mm",
        "Aluminum/glass 6/10/6",
        "Aluminum/glass 10 mm",
        "Aluminum/glass 8 mm",
        "Aluminum/glass 6 mm",
        "Ståldør inntil EI120",
        "Massivdør inntil EI60"
    ];
}
