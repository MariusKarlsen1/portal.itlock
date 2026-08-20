namespace PortalItlock.Web.Models;

public class SjekklistePunkt
{
    public int Id { get; set; }
    public int SjekklisteMalId { get; set; }
    public SjekklisteMal? SjekklisteMal { get; set; }

    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }
}
