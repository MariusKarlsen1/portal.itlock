namespace PortalItlock.Web.Models;

public class KundeHendelse
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public required string Beskrivelse { get; set; }

    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }
}
