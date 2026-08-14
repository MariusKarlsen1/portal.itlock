namespace PortalItlock.Web.Models;

public class Component
{
    public int Id { get; set; }
    public int ComponentTypeId { get; set; }
    public ComponentType? Type { get; set; }

    public required string Navn { get; set; }
    public string? Produsent { get; set; }
    public string? Produktkode { get; set; }
    public string? Beskrivelse { get; set; }

    public List<PackageComponent> Pakker { get; set; } = [];
}
