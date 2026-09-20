namespace PortalItlock.Web.Models;

public class ProsjektVedlegg
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public int? MappeId { get; set; }
    public ProsjektMappe? Mappe { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
