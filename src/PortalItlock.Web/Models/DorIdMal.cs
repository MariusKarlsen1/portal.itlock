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

    public string? FargeKarm { get; set; }
    public string? Dorkonstruksjon { get; set; }
    public string? FargeDorblad { get; set; }
    public string? Karmtype { get; set; }
    public string? Karmkonstruksjon { get; set; }
    public string? Terskel { get; set; }
    public string? Sparkeplate { get; set; }
    public int? AMal { get; set; }
    public int? BMal { get; set; }
    public bool? GlassIDor { get; set; }
    public string? Merknad { get; set; }

    // CE godkjenning info
    public string? CeDorblad { get; set; }
    public string? CeGlasstykkelse { get; set; }

    public List<Dor> Dorer { get; set; } = [];
}
