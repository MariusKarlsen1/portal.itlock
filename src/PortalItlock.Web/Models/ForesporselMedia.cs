namespace PortalItlock.Web.Models;

public class ForesporselMedia
{
    public int Id { get; set; }
    public int ForesporselId { get; set; }
    public Foresporsel? Foresporsel { get; set; }
    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }
}
