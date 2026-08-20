namespace PortalItlock.Web.Models;

public class ServicehenvendelseBilde
{
    public int Id { get; set; }
    public int ServicehenvendelseId { get; set; }
    public Servicehenvendelse? Servicehenvendelse { get; set; }

    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }
    public bool ErDokumentasjon { get; set; }
}
