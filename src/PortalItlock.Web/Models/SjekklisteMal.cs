namespace PortalItlock.Web.Models;

public class SjekklisteMal
{
    public int Id { get; set; }
    public required string Dortype { get; set; }

    public List<SjekklistePunkt> Punkter { get; set; } = [];
}
