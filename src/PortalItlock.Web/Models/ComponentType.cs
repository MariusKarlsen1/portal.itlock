namespace PortalItlock.Web.Models;

public class ComponentType
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public List<Component> Komponenter { get; set; } = [];
}
