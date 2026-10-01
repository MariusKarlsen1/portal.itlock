namespace PortalItlock.Web.Models;

public class PlattformBrukerPasswordResetToken
{
    public int Id { get; set; }
    public int PlattformBrukerId { get; set; }
    public PlattformBruker? PlattformBruker { get; set; }
    public required string Token { get; set; }
    public DateTime UtlopsDato { get; set; }
    public bool Brukt { get; set; }
}
