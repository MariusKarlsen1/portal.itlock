namespace PortalItlock.Web.Models;

public class KundeOppfolgingNotat
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public required string Tekst { get; set; }

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
