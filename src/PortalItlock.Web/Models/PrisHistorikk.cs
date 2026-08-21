namespace PortalItlock.Web.Models;

public class PrisHistorikk
{
    public int Id { get; set; }

    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public decimal? GammelPrisNetto { get; set; }
    public decimal? NyPrisNetto { get; set; }
    public decimal? GammelPrisVeiledende { get; set; }
    public decimal? NyPrisVeiledende { get; set; }

    public DateTime Dato { get; set; }
    public required string Kilde { get; set; }
}
