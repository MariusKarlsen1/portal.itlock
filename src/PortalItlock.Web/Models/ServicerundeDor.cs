namespace PortalItlock.Web.Models;

// Fase 4 serviceavtaler: én dør i en servicerunde som føres dør for dør.
// Dette er et fast øyeblikksbilde - dørnummer, rom, plassering på tegningen,
// kategori og komponentene kopieres inn når runden opprettes, så runden viser
// nøyaktig det som var montert da, selv om døren endres senere.
public class ServicerundeDor
{
    public int Id { get; set; }
    public int ServicerundeId { get; set; }
    public Servicerunde? Servicerunde { get; set; }

    // Den faste dør-ID-en i dørregisteret. Bevisst uten fremmednøkkel, så runden
    // står seg selv om døren slettes.
    public int DorId { get; set; }
    public required string Dornummer { get; set; }
    public string? Rom { get; set; }
    public string? Etasje { get; set; }
    public int? PlantegningId { get; set; }
    public string? PlantegningNavn { get; set; }
    public double? PosX { get; set; }
    public double? PosY { get; set; }
    public string? Kategori { get; set; }

    // JSON-liste med komponentene slik de var: navn, produktkode, komponenttype og servicetype.
    public string? KomponenterJson { get; set; }

    public string Status { get; set; } = ServicerundeDorStatus.IkkeKontrollert;
    public string? Notat { get; set; }
    public DateTime? KontrollertDato { get; set; }
    public int? KontrollertAvBrukerId { get; set; }
    public int Rekkefolge { get; set; }

    public List<ServicerundeDorPunkt> Punkter { get; set; } = [];
}

public class ServicerundeDorPunkt
{
    public int Id { get; set; }
    public int ServicerundeDorId { get; set; }
    public ServicerundeDor? ServicerundeDor { get; set; }
    public required string Tekst { get; set; }
    // Servicetypene punktet kommer fra, f.eks. "Dørautomatikk, Dørlukker".
    public string? Kilde { get; set; }
    // null = ikke kontrollert, ellers ServicerundeDorStatus.Ok / Avvik / IkkeRelevant.
    public string? Status { get; set; }
    public string? Kommentar { get; set; }
    public int Rekkefolge { get; set; }
}

public static class ServicerundeDorStatus
{
    public const string IkkeKontrollert = "ikke";
    public const string Ok = "ok";
    public const string Avvik = "avvik";
    public const string IkkeRelevant = "ir";
}

// Kontrollpunktene per servicetype. Punktene på en dør settes sammen fra
// servicetypene på døren, og like punkter slås sammen. Redigeres under
// Oppsett / Prisregler for service / Kontrollpunkter.
public class ServiceKontrollpunkt
{
    public int Id { get; set; }
    public required string Servicetype { get; set; }
    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }
}
