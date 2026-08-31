namespace PortalItlock.Web.Models;

public class DriftsmeldingMedia
{
    public int Id { get; set; }
    public int DriftsmeldingId { get; set; }
    public Driftsmelding? Driftsmelding { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }
}
