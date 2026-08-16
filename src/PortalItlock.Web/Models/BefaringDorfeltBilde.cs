namespace PortalItlock.Web.Models;

public class BefaringDorfeltBilde
{
    public int Id { get; set; }
    public int BefaringDorfeltId { get; set; }
    public BefaringDorfelt? Dorfelt { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
