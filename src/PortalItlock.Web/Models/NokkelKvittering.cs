namespace PortalItlock.Web.Models;

public class NokkelKvittering
{
    public int Id { get; set; }
    public int NokkelsystemId { get; set; }
    public Nokkelsystem? Nokkelsystem { get; set; }

    public DateTime Dato { get; set; } = DateTime.UtcNow;
    public required string MottakerNavn { get; set; }
    public string? NokkelBetegnelse { get; set; }
    public int Antall { get; set; } = 1;
    public string? RekvirertAv { get; set; }
    public string? Notater { get; set; }
}
