namespace PortalItlock.Web.Models;

// Innstillinger per organisasjon som plattformeier setter opp i
// adminportalen (/plattform/organisasjoner/{id}). Lagres i PlatformDbContext,
// som TenantLisens og TenantModul, så de finnes også når organisasjonens egen
// database ikke er åpen (bakgrunnsjobber, innkommende e-post).
public class TenantInnstillinger
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Organisasjonens e-postadresse er {EpostNavn}@full-kontroll.no, og
    // brukes både som avsender og som innboks. Unik på tvers av alle.
    public string? EpostNavn { get; set; }

    // Navnet kundene ser som avsender, f.eks. «Sørlandet Lås AS».
    public string? AvsenderNavn { get; set; }

    // Når en kunde svarer på en e-post: true = svaret kommer inn i portalen
    // (Forespørsler), false = svaret går til SvarTilEpost.
    public bool SvarTilPortal { get; set; } = true;
    public string? SvarTilEpost { get; set; }

    // Organisasjonens egen Tripletex: ansattnøkkelen (employee token) fra
    // firmaet, kryptert, én for produksjon og én for Tripletex sitt testmiljø.
    // TripletexBrukTest velger hvilken som brukes. Consumer-tokenet for
    // miljøet er plattformens (PlattformInnstillinger).
    public string? TripletexEmployeeTokenKryptert { get; set; }
    public string? TripletexTestEmployeeTokenKryptert { get; set; }
    public bool TripletexBrukTest { get; set; }
    public string? TripletexFirma { get; set; }
    public DateTime? TripletexTestet { get; set; }

    // Når den ukentlige påminnelsen om prosjektøkonomi sist ble sendt.
    public DateTime? SistOkonomiVarsel { get; set; }

    public DateTime SistEndret { get; set; } = DateTime.Now;
}

// Hvem i organisasjonen som får hvilke påminnelser på e-post.
public class TenantVarselMottaker
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public required string Epost { get; set; }
    // Fritekst som «Daglig leder» eller «Serviceavdelingen».
    public string? Beskrivelse { get; set; }

    public bool Service { get; set; } = true;
    public bool Lisens { get; set; } = true;
    public bool Ticket { get; set; } = true;
    public bool Prosjektokonomi { get; set; } = true;
}

public enum VarselType
{
    Service,
    Lisens,
    Ticket,
    Prosjektokonomi
}

public static class VarselTypeExtensions
{
    public static string Visningsnavn(this VarselType type) => type switch
    {
        VarselType.Service => "Serviceavtaler",
        VarselType.Lisens => "Lisenser",
        VarselType.Ticket => "Ticket-frister",
        VarselType.Prosjektokonomi => "Prosjektøkonomi",
        _ => type.ToString()
    };

    public static string Forklaring(this VarselType type) => type switch
    {
        VarselType.Service => "Serviceavtaler som forfaller innen 14 dager",
        VarselType.Lisens => "Kundelisenser som går ut snart",
        VarselType.Ticket => "Tickets uten eier som har passert fristen",
        VarselType.Prosjektokonomi => "Ukentlig: prosjekter over budsjett og etterkalkyler som venter",
        _ => ""
    };

    public static bool Gjelder(this TenantVarselMottaker m, VarselType type) => type switch
    {
        VarselType.Service => m.Service,
        VarselType.Lisens => m.Lisens,
        VarselType.Ticket => m.Ticket,
        VarselType.Prosjektokonomi => m.Prosjektokonomi,
        _ => false
    };
}
