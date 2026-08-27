namespace PortalItlock.Web.Models;

public class BefaringPdf
{
    public int Id { get; set; }
    public int BefaringId { get; set; }
    public Befaring? Befaring { get; set; }

    public required string Navn { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
