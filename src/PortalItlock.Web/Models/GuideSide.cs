namespace PortalItlock.Web.Models;

public class GuideSide
{
    public int Id { get; set; }
    public required string Nokkel { get; set; }
    public required string Tittel { get; set; }
    public string? Innhold { get; set; }

    public DateTime SistOppdatert { get; set; } = DateTime.UtcNow;
}
