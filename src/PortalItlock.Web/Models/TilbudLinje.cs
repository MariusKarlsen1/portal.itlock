namespace PortalItlock.Web.Models;

public class TilbudLinje
{
    public int Id { get; set; }
    public int TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }

    public int? ComponentId { get; set; }
    public Component? Component { get; set; }

    public required string Navn { get; set; }
    public decimal Innpris { get; set; }
    public decimal Utpris { get; set; }
    public int Antall { get; set; } = 1;
    public string? Enhet { get; set; }
    public int? MontasjeMinutter { get; set; }
    public LevertAv LevertAv { get; set; } = LevertAv.F;

    public TilbudPrisType? PrisType { get; set; }
    public decimal? Prosentsats { get; set; }
    public decimal RabattProsent { get; set; }
    public int Rekkefolge { get; set; }
}
