namespace PortalItlock.Web.Models;

public class FdvVedlegg
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    // Satt når PDF-en er FDV for en vare som mangler FDV i vareregisteret -
    // da kommer den med i FDV-en der varen ellers ville stått. Null = ekstra vedlegg.
    public int? ComponentId { get; set; }

    public required string Navn { get; set; }
    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
