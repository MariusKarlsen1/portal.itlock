namespace PortalItlock.Web.Models;

public class BrukerPasswordResetToken
{
    public int Id { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }

    public required string Token { get; set; }
    public DateTime UtlopsDato { get; set; }
    public bool Brukt { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
