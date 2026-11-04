namespace PortalItlock.Web.Models;

public class PlanForbindelse
{
    public int Id { get; set; }

    public int FraUtstyrId { get; set; }
    public PlanUtstyr? FraUtstyr { get; set; }

    public int TilUtstyrId { get; set; }
    public PlanUtstyr? TilUtstyr { get; set; }

    public KabelType Type { get; set; }

    // Punkter kabelen går gjennom mellom de to enhetene, i prosent av tegningen:
    // "x,y;x,y". Kabelen tegnes som en myk kurve gjennom dem.
    public string? Knekkpunkter { get; set; }
    public string? Notat { get; set; }
}
