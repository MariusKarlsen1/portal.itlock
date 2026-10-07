namespace PortalItlock.Web.Models;

public class NokkelsystemHendelse
{
    public int Id { get; set; }
    public int NokkelsystemId { get; set; }
    public Nokkelsystem? Nokkelsystem { get; set; }

    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public required string Beskrivelse { get; set; }

    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }
}
