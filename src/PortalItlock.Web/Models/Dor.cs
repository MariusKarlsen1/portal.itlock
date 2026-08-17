namespace PortalItlock.Web.Models;

public class Dor
{
    public int Id { get; set; }
    public int PlantegningId { get; set; }
    public Plantegning? Plantegning { get; set; }

    public required string Dornummer { get; set; }

    // Posisjon på plantegningen, i prosent (0-100) av bildets bredde/høyde
    public double PosX { get; set; }
    public double PosY { get; set; }

    public string? Etasje { get; set; }
    public string? Sone { get; set; }
    public string? Dortype { get; set; }
    public string? BxH { get; set; }
    public string? Slagretning { get; set; }
    public string? Notater { get; set; }

    public bool FerdigMontert { get; set; }
    public DateTime? MontertDato { get; set; }

    public List<DorKomponent> Komponenter { get; set; } = [];
}
