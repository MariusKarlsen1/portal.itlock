namespace PortalItlock.Web.Models;

public class KundeDokument
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }
    public string? Type { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
