namespace PortalItlock.Web.Models;

public class CeGodkjenningMedia
{
    public int Id { get; set; }
    public int CeGodkjenningId { get; set; }
    public CeGodkjenning? CeGodkjenning { get; set; }

    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }
    public CeGodkjenningMediaPlassering Plassering { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
