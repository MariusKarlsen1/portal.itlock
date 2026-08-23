namespace PortalItlock.Web.Models;

public class ArbeidsordreMedia
{
    public int Id { get; set; }
    public int ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public bool LastetOppAvKunde { get; set; }
}
