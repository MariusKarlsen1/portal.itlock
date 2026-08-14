namespace PortalItlock.Web.Models;

public class Package
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }

    public bool ErManuell { get; set; }
    public string? OpprettetAv { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<PackageRequirement> Krav { get; set; } = [];
    public List<PackageComponent> Komponenter { get; set; } = [];
}
