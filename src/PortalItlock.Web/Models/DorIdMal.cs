namespace PortalItlock.Web.Models;

public class DorIdMal
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Kode { get; set; }
    public string? Brann { get; set; }
    public string? Lyd { get; set; }
    public bool? FriBredde086 { get; set; }
    public int? Bredde { get; set; }
    public int? Hoyde { get; set; }
    public string? Dortype { get; set; }

    public List<Dor> Dorer { get; set; } = [];
}
