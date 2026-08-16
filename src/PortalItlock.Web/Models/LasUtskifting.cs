namespace PortalItlock.Web.Models;

public class LasUtskifting
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public DateTime? Dato { get; set; }

    // Dør
    public string? TotalMm { get; set; }
    public string? UtsideMm { get; set; }
    public string? InnsideMm { get; set; }
    public string? AvstandDorkantTilSkiltMm { get; set; }
    public string? DorFabrikat { get; set; }
    public string? DorNummer { get; set; }
    public string? Stolpehoyde { get; set; }
    public string? Stolpebredde { get; set; }
    public string? Overflatebehandling { get; set; }
    public string? OverflatebehandlingAnnet { get; set; }

    // Karm
    public string? KarmFabrikat { get; set; }
    public string? KarmNummer { get; set; }
    public string? KarmAvstand { get; set; }
    public string? Skruer { get; set; }

    // Hengsling av dør
    public string? HengslingSide { get; set; }
    public string? Slagretning { get; set; }

    public string? Merknad { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
