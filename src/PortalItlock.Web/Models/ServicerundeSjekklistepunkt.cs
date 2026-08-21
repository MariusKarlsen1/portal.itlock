namespace PortalItlock.Web.Models;

public class ServicerundeSjekklistepunkt
{
    public int Id { get; set; }
    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }
}
