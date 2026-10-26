namespace PortalItlock.Web.Models;

// Planlagt oppfølging av en kunde (kundekort → Oppfølging). Når fristen
// nærmer seg vises den i varslene (bjella) og på Hjem - Kunde.NesteOppfolgingsDato
// holdes oppdatert med tidligste åpne oppfølging så eksisterende varsler virker.
public class KundeOppfolging
{
    public int Id { get; set; }

    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public required string Tittel { get; set; }
    public KundeOppfolgingType Type { get; set; } = KundeOppfolgingType.Ring;

    // Dato og klokkeslett for oppfølgingen (lokal tid).
    public DateTime Frist { get; set; }

    public int? AnsvarligBrukerId { get; set; }
    public Bruker? AnsvarligBruker { get; set; }

    public string? Kontaktperson { get; set; }
    public string? Notat { get; set; }

    public bool VarselPortal { get; set; } = true;
    public bool VarselEpost { get; set; } = true;
    public bool VarselSms { get; set; }

    // 0 = på dagen, 1 = dagen før, 7 = en uke før.
    public int VarselDagerFor { get; set; }
    public KundeOppfolgingGjenta Gjenta { get; set; } = KundeOppfolgingGjenta.Ikke;

    public bool EpostVarselSendt { get; set; }

    public bool Utfort { get; set; }
    public DateTime? UtfortDato { get; set; }
    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }
    public string? UtfortKommentar { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public DateTime VarselDato => Frist.Date.AddDays(-VarselDagerFor);
}

public enum KundeOppfolgingType { Ring, Epost, Mote, Befaring, Annet }

public enum KundeOppfolgingGjenta { Ikke, Maaned, Kvartal, Aar }

public static class KundeOppfolgingExtensions
{
    // Navn på ikonet (Icon.razor) for typen.
    public static string Ikon(this KundeOppfolgingType t) => t switch
    {
        KundeOppfolgingType.Ring => "phone",
        KundeOppfolgingType.Epost => "mail",
        KundeOppfolgingType.Mote => "users",
        KundeOppfolgingType.Befaring => "search",
        _ => "edit"
    };

    public static string Visningsnavn(this KundeOppfolgingType t) => t switch
    {
        KundeOppfolgingType.Ring => "Ring",
        KundeOppfolgingType.Epost => "E-post",
        KundeOppfolgingType.Mote => "Møte",
        KundeOppfolgingType.Befaring => "Befaring",
        _ => "Annet"
    };

    public static string Visningsnavn(this KundeOppfolgingGjenta g) => g switch
    {
        KundeOppfolgingGjenta.Maaned => "Hver måned",
        KundeOppfolgingGjenta.Kvartal => "Hvert kvartal",
        KundeOppfolgingGjenta.Aar => "Hvert år",
        _ => "Ikke"
    };

    public static DateTime NesteGang(this KundeOppfolgingGjenta g, DateTime fra) => g switch
    {
        KundeOppfolgingGjenta.Maaned => fra.AddMonths(1),
        KundeOppfolgingGjenta.Kvartal => fra.AddMonths(3),
        KundeOppfolgingGjenta.Aar => fra.AddYears(1),
        _ => fra
    };
}
