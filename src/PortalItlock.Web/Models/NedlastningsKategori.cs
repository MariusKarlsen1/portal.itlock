namespace PortalItlock.Web.Models;

public class NedlastningsKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public string Farge { get; set; } = "brun";
    public int Rekkefolge { get; set; }

    public List<NedlastningsFil> Filer { get; set; } = [];
}
