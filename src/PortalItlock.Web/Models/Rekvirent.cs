namespace PortalItlock.Web.Models;

public class Rekvirent
{
    public int Id { get; set; }
    public int NokkelsystemId { get; set; }
    public Nokkelsystem? Nokkelsystem { get; set; }

    public required string Navn { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Rolle { get; set; }
    public string? Notater { get; set; }
}
