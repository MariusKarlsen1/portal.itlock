namespace PortalItlock.Web.Models;

public class ForesporselNotat
{
    public int Id { get; set; }
    public int ForesporselId { get; set; }
    public Foresporsel? Foresporsel { get; set; }

    public required string Tekst { get; set; }

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
