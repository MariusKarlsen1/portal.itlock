namespace PortalItlock.Web.Models;

public class Component
{
    public int Id { get; set; }
    public int? ComponentTypeId { get; set; }
    public ComponentType? Type { get; set; }

    public required string Navn { get; set; }
    public string? Produsent { get; set; }
    public string? Produktkode { get; set; }
    public string? Beskrivelse { get; set; }

    public string? Leverandor { get; set; }
    public string? Varegruppe { get; set; }
    public decimal? PrisNetto { get; set; }
    public decimal? PrisVeiledende { get; set; }
    public int? MontasjeMinutter { get; set; }
    public bool Aktiv { get; set; } = true;

    public List<PackageComponent> Pakker { get; set; } = [];
}
