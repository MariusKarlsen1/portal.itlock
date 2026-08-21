namespace PortalItlock.Web.Models;

public class PlanUtstyr
{
    public int Id { get; set; }
    public int PlantegningId { get; set; }
    public Plantegning? Plantegning { get; set; }

    public PlanUtstyrType Type { get; set; }
    public double PosX { get; set; }
    public double PosY { get; set; }

    public string? Notat { get; set; }
}
