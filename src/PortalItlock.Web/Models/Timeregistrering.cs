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
    public string? Kommentar { get; set; }
    public decimal Kilometer { get; set; }

    [NotMapped]
    public decimal TotalTimer => Math.Max(0, (decimal)(Slutt - Start).TotalHours - PauseMinutter / 60m);
}
