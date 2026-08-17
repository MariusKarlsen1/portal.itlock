namespace PortalItlock.Web.Models;

public class DorKomponent
{
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int Antall { get; set; } = 1;

    public bool Montert { get; set; }
    public DateTime? MontertDato { get; set; }
}
