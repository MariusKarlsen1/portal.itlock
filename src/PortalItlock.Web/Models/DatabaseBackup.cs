namespace PortalItlock.Web.Models;

public class DatabaseBackup
{
    public int Id { get; set; }
    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public bool Vellykket { get; set; }
    public string? Melding { get; set; }
    public long? StorrelseBytes { get; set; }
}
