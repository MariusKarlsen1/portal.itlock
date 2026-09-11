namespace PortalItlock.Web.Models;

public class ArbeidsordreVare
{
    public int Id { get; set; }
    public int ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public int? ComponentId { get; set; }
    public Component? Component { get; set; }

    public required string Navn { get; set; }
    public int Antall { get; set; } = 1;
    public decimal Kostpris { get; set; }
    public decimal Utpris { get; set; }

    public int? LagtTilAvBrukerId { get; set; }
    public Bruker? LagtTilAvBruker { get; set; }
    public DateTime? LagtTilDato { get; set; }
}
