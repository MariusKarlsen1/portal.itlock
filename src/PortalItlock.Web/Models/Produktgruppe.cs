namespace PortalItlock.Web.Models;

public class Produktgruppe
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public List<Component> Komponenter { get; set; } = [];
    public List<KundeRabatt> KundeRabatter { get; set; } = [];
}
