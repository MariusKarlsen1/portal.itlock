namespace PortalItlock.Web.Models;

public class Foresporsel
{
    public int Id { get; set; }
    public string FraEpost { get; set; } = "";
    public string? FraNavn { get; set; }
    public string Emne { get; set; } = "";
    public string Innhold { get; set; } = "";
    public DateTime MottattDato { get; set; } = DateTime.Now;
    public bool Lest { get; set; }
    public string RawJson { get; set; } = "";
    public List<ForesporselMedia> Media { get; set; } = [];
}
