namespace PortalItlock.Web.Models;

public class TilbudHendelse
{
    public int Id { get; set; }
    public int TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }
    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public required string Beskrivelse { get; set; }
    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }
}
