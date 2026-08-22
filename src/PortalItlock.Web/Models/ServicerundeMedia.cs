namespace PortalItlock.Web.Models;

public class ServicerundeMedia
{
    public int Id { get; set; }
    public int ServicerundeId { get; set; }
    public Servicerunde? Servicerunde { get; set; }

    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
