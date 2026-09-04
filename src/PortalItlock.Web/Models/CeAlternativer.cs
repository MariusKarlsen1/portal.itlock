namespace PortalItlock.Web.Models;

// Faste valglister til CE-godkjenning, delt mellom prosjekt-/dørskjemaene (norsk visning)
// og CE-veiviseren (engelsk visning av de samme underliggende verdiene).
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

    public static readonly string[] DekningsomradeAlternativer = ["No coverage", "Full width door", "According to annex G"];

    public static readonly string[] GlassIDorAlternativer = ["With glass", "Without glass"];

    public static readonly string[] TypeAvGlassAlternativer =
        ["No glass", "Tempered", "Laminated", "Safety film", "Untreated glass", "Wired glass", "Tempered/Laminated"];

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

    private static readonly Dictionary<string, string> ByggkategoriEngelsk = new()
    {
        ["Barnehage / barneskole"] = "Kindergarten / Primary school",
        ["Barnehage / barneskole personalområde"] = "Kindergarten / Primary school - Staff area",
        ["Ungdomsskole / Vgs / universitet"] = "Secondary school / College / University",
        ["Offentlig - Publikumsområde"] = "Public - Public area",
        ["Offentlig - Personalområde"] = "Public - Staff area",
        ["Kontor - Publikumsområde"] = "Office - Public area",
        ["Kontor - Personalområde"] = "Office - Staff area",
        ["Publikumsområde"] = "Public area",
        ["Bolig"] = "Residential",
        ["Helse / omsorg - Pasientområde"] = "Health / care - Patient area",
        ["Helse / omsorg - Personalområde"] = "Health / care - Staff area",
        ["Eldrehjem / omsorgsbolig"] = "Nursing home / care housing",
        ["Annet - Høy risiko"] = "Other - High risk",
        ["Annet - Tolerert risiko"] = "Other - Tolerated risk"
    };

    private static readonly Dictionary<string, string> RisikoklasseEngelsk = new()
    {
        ["Høy risiko"] = "High risk",
        ["Tolerert risiko"] = "Tolerated risk"
    };

    private static readonly Dictionary<string, string> EnergiKlasseEngelsk = new()
    {
        ["Lav"] = "Low",
        ["Høy"] = "High"
    };

    public static string ByggkategoriTilEngelsk(string? verdi) =>
        verdi is not null && ByggkategoriEngelsk.TryGetValue(verdi, out var t) ? t : verdi ?? "";

    public static string RisikoklasseTilEngelsk(string? verdi) =>
        verdi is not null && RisikoklasseEngelsk.TryGetValue(verdi, out var t) ? t : verdi ?? "";

    public static string EnergiKlasseTilEngelsk(string? verdi) =>
        verdi is not null && EnergiKlasseEngelsk.TryGetValue(verdi, out var t) ? t : verdi ?? "";
}
