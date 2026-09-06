namespace PortalItlock.Web.Models;

public class DorHendelse
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public required string Beskrivelse { get; set; }

    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }
}
