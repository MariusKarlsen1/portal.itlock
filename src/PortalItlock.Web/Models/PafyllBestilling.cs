namespace PortalItlock.Web.Models;

// Montøren ber om påfyll av bilen fra hovedlageret i montørappen. Lageret
// plukker varene og legger dem klare; varene flyttes fra hovedlageret til
// bilen først når montøren bekrefter at de er hentet, så beholdningen alltid
// viser hvor varene faktisk er.
public enum PafyllStatus
{
    Sendt = 0,
    Plukkes = 1,
    Klar = 2,
    Hentet = 3,
    Avbrutt = 4
}

public class PafyllBestilling
{
    public int Id { get; set; }

    public int BilLagerId { get; set; }
    public Lager? BilLager { get; set; }

    public int? BrukerId { get; set; }
    public string? BrukerNavn { get; set; }

    public PafyllStatus Status { get; set; }
    public DateTime Opprettet { get; set; } = DateTime.Now;
    public DateTime? KlarDato { get; set; }
    public string? PlukketAv { get; set; }
    public DateTime? HentetDato { get; set; }

    public List<PafyllLinje> Linjer { get; set; } = [];
}

public class PafyllLinje
{
    public int Id { get; set; }
    public int PafyllBestillingId { get; set; }
    public PafyllBestilling? PafyllBestilling { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int Antall { get; set; }

    // Hvor mange lageret har plukket (kan være færre enn bestilt).
    public int Plukket { get; set; }
}
