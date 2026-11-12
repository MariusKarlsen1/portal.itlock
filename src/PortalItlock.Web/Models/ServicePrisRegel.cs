namespace PortalItlock.Web.Models;

// Regel som setter servicekategori og pris på en dør ut fra komponentene
// (servicetypene) og dørfunksjonene som er registrert på den. Reglene er felles
// for organisasjonen og brukes av alle serviceavtaler med prismodellen
// "Kategorier etter regler". Den regelen med lavest prioritet som treffer
// gjelder - en dør havner aldri i to kategorier. Se ServiceRegelMotor.
public class ServicePrisRegel
{
    public int Id { get; set; }

    // Navnet er også kategorinavnet som vises på prising og i tilbudet.
    public required string Navn { get; set; }

    // 1 = sjekkes først. Like prioriteter som treffer samme dør gir konflikt.
    public int Prioritet { get; set; } = 5;

    // true = alle "har"-betingelsene må være oppfylt (OG), false = minst én (ELLER).
    // "Har ikke"-betingelser (ekskluderinger) må alltid være oppfylt.
    public bool KreverAlle { get; set; } = true;

    // «Bare disse»: døren har minst én av komponenttypene i regelen og ingen andre.
    // F.eks. Mekanisk = bare sylinder, dørvrider, skilt – en dør som også har
    // automatikk faller utenfor. Når true brukes ikke KreverAlle.
    public bool BareDisse { get; set; }

    // Pris per dør per servicerunde (eks. mva).
    public decimal Pris { get; set; }

    // Estimert tid per dør per runde - brukes til estimert tid og dekningsbidrag.
    public int Minutter { get; set; }

    // Kort tekst som kan vises under kategorien i tilbudet, f.eks. "inkl. resertifisering av CE".
    public string? Undertekst { get; set; }

    public bool Aktiv { get; set; } = true;

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public DateTime? EndretDato { get; set; }

    public List<ServicePrisRegelBetingelse> Betingelser { get; set; } = [];
}

public class ServicePrisRegelBetingelse
{
    public int Id { get; set; }
    public int ServicePrisRegelId { get; set; }
    public ServicePrisRegel? Regel { get; set; }

    // Hva betingelsen ser på - se ServiceRegelFelt.
    public string Felt { get; set; } = ServiceRegelFelt.Servicetype;

    // Servicetype (f.eks. "Dørautomatikk") eller tekst som dørfunksjonen skal inneholde.
    public required string Verdi { get; set; }

    // true = døren skal IKKE ha dette (ekskludering).
    public bool ErIkke { get; set; }

    public int Rekkefolge { get; set; }

    // Krav-gruppe (P1 Prising av dører): betingelser med samme gruppe (> 0) er
    // «en av» – minst én må stemme. Alle gruppene må stemme («og»). Gruppe 0 =
    // hver betingelse er et eget krav (slik reglene var før).
    public int Gruppe { get; set; }
}

public static class ServiceRegelFelt
{
    public const string Servicetype = "servicetype";
    public const string Funksjon = "funksjon";
}
