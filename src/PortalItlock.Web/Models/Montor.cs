namespace PortalItlock.Web.Models;

public class Montor
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public List<Arbeidsordre> Arbeidsordre { get; set; } = [];
    public List<Timeregistrering> Timeregistreringer { get; set; } = [];
}
