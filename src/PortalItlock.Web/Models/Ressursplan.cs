namespace PortalItlock.Web.Models;

// En "boks" i ressursplanleggeren - en plassholder for at en montør er satt av
// til noe en gitt dag, uavhengig av om det finnes en arbeidsordre ennå. Vises
// også i Kalender.razor for den aktuelle montøren.
public class Ressursplan
{
    public int Id { get; set; }
    public DateTime Dato { get; set; }
    public TimeSpan Fra { get; set; }
    public TimeSpan Til { get; set; }

    public int MontorId { get; set; }
    public Bruker? Montor { get; set; }

    public required string Info { get; set; }

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
