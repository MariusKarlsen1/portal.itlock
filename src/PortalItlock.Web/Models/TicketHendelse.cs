namespace PortalItlock.Web.Models;

public class TicketHendelse
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public required string Beskrivelse { get; set; }
    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }
    public bool ErKundeSynlig { get; set; }
}
