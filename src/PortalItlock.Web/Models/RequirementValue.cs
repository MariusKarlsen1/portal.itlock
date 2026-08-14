namespace PortalItlock.Web.Models;

public class RequirementValue
{
    public int Id { get; set; }
    public int RequirementDimensionId { get; set; }
    public RequirementDimension? Dimensjon { get; set; }

    public required string Verdi { get; set; }
    public string? Kode { get; set; }
    public int Rekkefolge { get; set; }

    public List<PackageRequirement> Pakker { get; set; } = [];
}
