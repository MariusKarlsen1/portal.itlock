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

    public bool Plukket { get; set; }
    public DateTime? PlukketDato { get; set; }
    public int? PlukketAvBrukerId { get; set; }
    public Bruker? PlukketAvBruker { get; set; }

    // Lageret varen ble tatt fra (montørens bil). Da er den allerede ført som
    // forbruk på prosjektet, og endret antall eller fjerning justerer lageret.
    public int? FraLagerId { get; set; }
}
