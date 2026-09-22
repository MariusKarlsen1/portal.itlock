namespace PortalItlock.Web.Models;

public class NedlastningsKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public int Rekkefolge { get; set; }

    public List<NedlastningsFil> Filer { get; set; } = [];
}
