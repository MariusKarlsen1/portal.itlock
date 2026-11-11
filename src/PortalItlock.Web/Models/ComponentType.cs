namespace PortalItlock.Web.Models;

public class ComponentType
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    // Styrer rekkefølgen komponenttypen vises i overalt (nedtrekksmenyer på
    // varer m.m.) - lavest tall (fra 1) vises først.
    public int Sortering { get; set; }

    // Standard servicetype (f.eks. "Dørautomatikk", "Elektrisk sluttstykke") som
    // prisreglene for serviceavtaler ser på. Null = utledes fra navnet, se
    // ServiceRegelMotor.StandardServicetype. Settes under Oppsett / Prisregler for service.
    public string? Servicetype { get; set; }

    public CeTilbehorKategori CeKategori { get; set; } = CeTilbehorKategori.Ingen;
    public byte[]? CeDokumentData { get; set; }
    public string? CeDokumentFilnavn { get; set; }
    public string? CeDokumentContentType { get; set; }

    public List<Component> Komponenter { get; set; } = [];
}
