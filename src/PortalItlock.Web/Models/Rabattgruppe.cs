namespace PortalItlock.Web.Models;

public class Rabattgruppe
{
    public int Id { get; set; }
    public required string Kode { get; set; }
    public required string Navn { get; set; }
    public required string Leverandor { get; set; }
    public string? Beskrivelse { get; set; }
    public decimal RabattProsent { get; set; }
    public bool Aktiv { get; set; } = true;

    public List<Component> Komponenter { get; set; } = [];
}
