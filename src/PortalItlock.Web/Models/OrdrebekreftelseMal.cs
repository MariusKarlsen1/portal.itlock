using System.Text.Json;

namespace PortalItlock.Web.Models;

// Malen for ordrebekreftelsen (Oppsett og maler → Ordrebekreftelse). Én rad per
// organisasjon. Standardteksten ligger i ObMalInnhold.Standard(); det organisasjonen
// endrer lagres som JSON. Gjelder ordrebekreftelser fra både tilbud og arbeidsordre.
public class OrdrebekreftelseMal
{
    public int Id { get; set; }
    public string? Json { get; set; }
}

public sealed class ObMalInnhold
{
    // {firma} i teksten byttes ut med firmanavnet når PDF-en lages.
    public const string Firma = "{firma}";

    public List<ObMalFelt> Felt { get; set; } = [];
    public string Takk { get; set; } = "";
    public List<ObMalPunkt> Punkter { get; set; } = [];
    public bool Kontakt { get; set; } = true;
    public string Bunntekst { get; set; } = "";

    public static ObMalInnhold Standard() => new()
    {
        Felt =
        [
            new("kundenr", "Kundenummer", true),
            new("deres", "Deres referanse", true),
            new("var", "Vår referanse", true),
            new("levmate", "Leveringsmåte", true, ""),
            new("levtid", "Leveringstid", true, "Avtales nærmere"),
            new("betaling", "Betalingsbetingelser", true, "Iht. faktura"),
            new("tilbud", "Tilbud nr.", true),
            new("godkjent", "Godkjent dato", true),
            new("ordre", "Arbeidsordre", true),
            new("prosjektid", "Prosjekt ID", false),
            new("prosjektnavn", "Prosjektnavn", false),
        ],
        Takk = "Takk for bestillingen! Tidspunkt for levering og montering avtales nærmere – montøren ringer før ankomst. Sørg for at det er ryddet og fri tilgang til døren/området.",
        Punkter =
        [
            new("Salgspant og eiendomsrett", $"{Firma} forbeholder seg salgspant i de leverte varene som sikkerhet for kjøpesummen, inkludert renter og omkostninger, i medhold av panteloven §§ 3-14 til 3-22. Eiendomsretten går over til kjøper først når hele kjøpesummen er betalt."),
            new("Spesialtilpassede varer", "Det gis ikke retur- eller avbestillingsrett på spesialtilpassede systemlåser, lisensierte nøkler eller prosjektbestilte låsvarer etter at ordren er satt i produksjon eller bekreftet bestilt hos produsent."),
            new("Avbestilling av tjenester", "Avbestilling eller endring av avtalt montering må skje senest 24 timer før oppmøte. Ved for sen avbestilling eller manglende tilgang påløper bomtur etter gjeldende satser."),
            new("Angrerett (forbrukerkjøp)", "14 dagers angrerett på standard lagervarer i ubrukt stand og originalemballasje. Angreretten bortfaller for tjenester (som montering) når arbeidet er påbegynt eller fullført etter avtale.") { KunPrivat = true },
        ],
        Kontakt = true,
        Bunntekst = "Ordren leveres i henhold til betingelsene i denne ordrebekreftelsen.",
    };

    // Leser lagret mal. Felt som er kommet til i standarden senere, legges til.
    public static ObMalInnhold Les(string? json)
    {
        var standard = Standard();
        if (string.IsNullOrWhiteSpace(json))
        {
            return standard;
        }
        ObMalInnhold? mal;
        try
        {
            mal = JsonSerializer.Deserialize<ObMalInnhold>(json);
        }
        catch (JsonException)
        {
            mal = null;
        }
        if (mal is null)
        {
            return standard;
        }
        foreach (var f in standard.Felt.Where(f => mal.Felt.All(m => m.Nokkel != f.Nokkel)))
        {
            mal.Felt.Add(f);
        }
        return mal;
    }

    public string Skriv() => JsonSerializer.Serialize(this);

    public ObMalFelt? FeltFor(string nokkel) => Felt.FirstOrDefault(f => f.Nokkel == nokkel);
}

public sealed class ObMalFelt
{
    public ObMalFelt() { }

    public ObMalFelt(string nokkel, string navn, bool vis, string? verdi = null)
    {
        Nokkel = nokkel;
        Navn = navn;
        Vis = vis;
        Verdi = verdi;
    }

    public string Nokkel { get; set; } = "";
    public string Navn { get; set; } = "";
    public bool Vis { get; set; } = true;
    // Standardverdi der det er aktuelt (Leveringstid uten dato, Betalingsbetingelser).
    public string? Verdi { get; set; }
}

public sealed class ObMalPunkt
{
    public ObMalPunkt() { }

    public ObMalPunkt(string tittel, string tekst)
    {
        Tittel = tittel;
        Tekst = tekst;
    }

    public string Tittel { get; set; } = "";
    public string Tekst { get; set; } = "";
    public bool Vis { get; set; } = true;
    public bool KunPrivat { get; set; }
    public bool Egen { get; set; }
}
