namespace PortalItlock.Web.Models;

public class SjekklistePdf
{
    public int Id { get; set; }
    public int ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public required string Navn { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
