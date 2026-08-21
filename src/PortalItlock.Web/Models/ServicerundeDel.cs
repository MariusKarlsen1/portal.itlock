namespace PortalItlock.Web.Models;

public class ServicerundeDel
{
    public int Id { get; set; }
    public int ServicerundeId { get; set; }
    public Servicerunde? Servicerunde { get; set; }

    public int? DorId { get; set; }
    public Dor? Dor { get; set; }

    public required string Beskrivelse { get; set; }
    public string? Feil { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
}
