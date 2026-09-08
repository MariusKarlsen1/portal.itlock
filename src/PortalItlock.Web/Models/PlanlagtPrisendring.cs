namespace PortalItlock.Web.Models;

public class PlanlagtPrisendring
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public decimal? GammelPrisNetto { get; set; }
    public decimal? GammelPrisVeiledende { get; set; }
    public decimal? NyPrisNetto { get; set; }
    public decimal? NyPrisVeiledende { get; set; }

    public DateTime GjelderFraDato { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public bool Utfort { get; set; }
    public string? Kilde { get; set; }
}
