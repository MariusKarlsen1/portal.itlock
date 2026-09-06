namespace PortalItlock.Web.Models;

public class ServiceKategoriPris
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Kategori { get; set; }
    public decimal? Pris { get; set; }
    public int? KravServicePrAar { get; set; }
    public int? ManueltAntall { get; set; }
    public bool ErEgendefinert { get; set; }
}
