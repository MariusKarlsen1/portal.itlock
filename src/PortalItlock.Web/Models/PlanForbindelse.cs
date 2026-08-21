namespace PortalItlock.Web.Models;

public class PlanForbindelse
{
    public int Id { get; set; }

    public int FraUtstyrId { get; set; }
    public PlanUtstyr? FraUtstyr { get; set; }

    public int TilUtstyrId { get; set; }
    public PlanUtstyr? TilUtstyr { get; set; }

    public KabelType Type { get; set; }
    public string? Notat { get; set; }
}
