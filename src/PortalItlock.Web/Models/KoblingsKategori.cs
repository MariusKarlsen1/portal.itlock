namespace PortalItlock.Web.Models;

public class KoblingsKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public int Rekkefolge { get; set; }

    public List<KoblingsSkjema> Skjemaer { get; set; } = [];
}
