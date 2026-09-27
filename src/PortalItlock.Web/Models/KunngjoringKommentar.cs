namespace PortalItlock.Web.Models;

public class KunngjoringKommentar
{
    public int Id { get; set; }
    public int KunngjoringId { get; set; }
    public Kunngjoring? Kunngjoring { get; set; }
    public required string Tekst { get; set; }
    public int? BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
