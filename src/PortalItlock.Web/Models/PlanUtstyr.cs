namespace PortalItlock.Web.Models;

public class PlanUtstyr
{
    public int Id { get; set; }
    public int PlantegningId { get; set; }
    public Plantegning? Plantegning { get; set; }

    public PlanUtstyrType Type { get; set; }
    public double PosX { get; set; }
    public double PosY { get; set; }

    // Størrelse som faktor av standardsymbolet (1 = vanlig). Bredde og høyde
    // hver for seg, så f.eks. et panikkbeslag kan gjøres lengre.
    public double Bredde { get; set; } = 1;
    public double Hoyde { get; set; } = 1;

    public string? Notat { get; set; }
}
