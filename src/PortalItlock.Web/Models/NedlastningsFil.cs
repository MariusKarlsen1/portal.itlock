namespace PortalItlock.Web.Models;

public class NedlastningsFil
{
    public int Id { get; set; }
    public int KategoriId { get; set; }
    public NedlastningsKategori? Kategori { get; set; }

    public required string Navn { get; set; }
    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
