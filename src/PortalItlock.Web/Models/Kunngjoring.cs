namespace PortalItlock.Web.Models;

public class Kunngjoring
{
    public int Id { get; set; }
    public required string Tittel { get; set; }
    public required string Innhold { get; set; }
    public KunngjoringKategori Kategori { get; set; } = KunngjoringKategori.Info;
    public bool Festet { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }
}
