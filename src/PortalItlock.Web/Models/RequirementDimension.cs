namespace PortalItlock.Web.Models;

public class RequirementDimension
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public int Rekkefolge { get; set; }

    public List<RequirementValue> Verdier { get; set; } = [];
}
