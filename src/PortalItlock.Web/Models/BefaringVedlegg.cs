namespace PortalItlock.Web.Models;

public class BefaringVedlegg
{
    public int Id { get; set; }
    public int BefaringId { get; set; }
    public Befaring? Befaring { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public bool ErVideo => ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
}
