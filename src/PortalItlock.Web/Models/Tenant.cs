namespace PortalItlock.Web.Models;

public enum TenantStatus
{
    Aktiv,
    Suspendert,
    Oppsagt
}

// Én rad per kunde i plattform-katalogen (PlatformDbContext) - helt adskilt fra
// kundens egne data, som ligger i en egen SQLite-fil pekt til av ConnectionString.
// ErStandard markerer hvilken tenant som brukes når ingen Subdomene matcher
// forespørselens Host-header (i dag: eneste tenant, og lokal utvikling).
public class Tenant
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Subdomene { get; set; }
    public required string ConnectionString { get; set; }
    public bool ErStandard { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Aktiv;
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public string? OrgNr { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Kundeansvarlig { get; set; }
    public TenantType? Type { get; set; }
    public DateTime? SistOppdatert { get; set; }

    // Merkevare - vises i organisasjonens EGEN portal (sidemeny-logo,
    // "Organisasjon"-navn under hurtigmenyen, og fargetemaet). Satt av
    // plattformeier på /plattform, ikke av organisasjonen selv. Tema er
    // en Id fra OrganisasjonTema.Alle (kuratert utvalg), ikke en fri hex-kode.
    public string? PortalVisningsnavn { get; set; }
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string Tema { get; set; } = OrganisasjonTema.Standard.Id;

    public List<TenantLisens> Lisenser { get; set; } = [];
}
