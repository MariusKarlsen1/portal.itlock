namespace PortalItlock.Web.Models;

public class DoorEnvironmentDocument
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public required string FileName { get; set; }
    public int Rekkefolge { get; set; }
}
