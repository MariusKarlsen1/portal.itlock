namespace PortalItlock.Web.Models;

public class DorKomponent
{
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int Antall { get; set; } = 1;
    public string? Enhet { get; set; }

    public bool Montert { get; set; }
    public DateTime? MontertDato { get; set; }

    public int? MontertAvBrukerId { get; set; }
    public Bruker? MontertAvBruker { get; set; }
}
