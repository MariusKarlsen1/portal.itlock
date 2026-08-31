namespace PortalItlock.Web.Models;

public class KoblingsSkjema
{
    public int Id { get; set; }
    public int KategoriId { get; set; }
    public KoblingsKategori? Kategori { get; set; }
    public required string Navn { get; set; }

    // Valgfri kobling til et prosjekt - skjemaet vises da som vedlegg på prosjektet.
    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
    public DateTime? OppdatertDato { get; set; }

    public List<KoblingsSymbol> Symboler { get; set; } = [];
    public List<KoblingsStrek> Streker { get; set; } = [];
}
