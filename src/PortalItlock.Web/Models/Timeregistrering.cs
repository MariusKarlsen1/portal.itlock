using System.ComponentModel.DataAnnotations.Schema;

namespace PortalItlock.Web.Models;

public class Timeregistrering
{
    public int Id { get; set; }

    public int ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public int MontorId { get; set; }
    public Bruker? Montor { get; set; }

    public DateTime Dato { get; set; } = DateTime.Today;
    public TimeSpan Start { get; set; }
    public TimeSpan Slutt { get; set; }
    public int PauseMinutter { get; set; }
    public TimeregistreringType Type { get; set; } = TimeregistreringType.NormalArbeidstid;

    // Hva timene gikk med til (Montering, Programmering, Service ...). Vises
    // som egen kolonne i timelista og styrer fargen i ukekalenderen.
    public TimeAktivitet Aktivitet { get; set; } = TimeAktivitet.Montering;

    // Om timene skal viderefaktureres kunden. Standard er ja - interne timer
    // må aktivt hukes av.
    public bool Fakturerbar { get; set; } = true;

    public string? Kommentar { get; set; }
    public decimal Kilometer { get; set; }

    public TimeregistreringStatus Status { get; set; } = TimeregistreringStatus.Venter;
    public DateTime? BehandletDato { get; set; }
    public int? BehandletAvBrukerId { get; set; }
    public Bruker? BehandletAvBruker { get; set; }

    [NotMapped]
    public decimal TotalTimer => Math.Max(0, (decimal)(Slutt - Start).TotalHours - PauseMinutter / 60m);
}
