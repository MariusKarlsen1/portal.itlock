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
}
