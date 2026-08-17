namespace PortalItlock.Web.Models;

public class Plantegning
{
    public int Id { get; set; }
    public int NokkelsystemId { get; set; }
    public Nokkelsystem? Nokkelsystem { get; set; }

    public required string Navn { get; set; }
    public string? Byggetrinn { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Dor> Dorer { get; set; } = [];
}
