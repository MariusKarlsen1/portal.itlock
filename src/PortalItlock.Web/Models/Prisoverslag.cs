namespace PortalItlock.Web.Models;

public class Prisoverslag
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Kunde { get; set; }

    public decimal AntallTimer { get; set; }
    public decimal Timepris { get; set; }
    public decimal PaslagProsent { get; set; }

    public string? Notater { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<PrisoverslagLinje> Linjer { get; set; } = [];
}
