namespace PortalItlock.Web.Models;

public class DorMedia
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }
    public DorMediaTidspunkt Tidspunkt { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
