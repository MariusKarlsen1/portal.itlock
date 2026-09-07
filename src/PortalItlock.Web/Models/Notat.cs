namespace PortalItlock.Web.Models;

public class Notat
{
    public int Id { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public required string Tekst { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
