namespace PortalItlock.Web.Models;

// En vare kan selges av flere leverandører, hver med sitt eget varenummer og
// pris (netto/veiledende) - ErStandard peker ut hvilken av dem som er
// gjeldende, og de prisene (+ varenummer) speiles da på selve Component
// (Leverandor/Produktkode/PrisNetto/PrisVeiledende), se Services/LeverandorSync.cs.
public class ComponentLeverandor
{
    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int LeverandorId { get; set; }
    public Leverandor? Leverandor { get; set; }

    public string? Varenummer { get; set; }
    public decimal? PrisNetto { get; set; }
    public decimal? PrisVeiledende { get; set; }
    public bool ErStandard { get; set; }
}
