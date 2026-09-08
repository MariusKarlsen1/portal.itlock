namespace PortalItlock.Web.Models;

public class Importhistorikk
{
    public int Id { get; set; }
    public required string Kilde { get; set; }
    public string? Filnavn { get; set; }
    public string? Leverandor { get; set; }

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public string Status { get; set; } = "Pågår";
    public int? AntallRader { get; set; }
    public int? AntallNye { get; set; }
    public int? AntallOppdatert { get; set; }
}
