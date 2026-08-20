namespace PortalItlock.Web.Models;

public class Nokkel
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Navn { get; set; }
    public string? Merking { get; set; }
    public string? Materiale { get; set; }
    public int Antall { get; set; }
    public int Rekkefolge { get; set; }
}
