namespace PortalItlock.Web.Models;

public class PackageComponent
{
    public int PackageId { get; set; }
    public Package? Package { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int Antall { get; set; } = 1;
}
