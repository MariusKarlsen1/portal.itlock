namespace PortalItlock.Web.Models;

public class Nyhet
{
    public int Id { get; set; }
    public required string Tittel { get; set; }
    public required string Innhold { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public string? CommitSha { get; set; }
}
