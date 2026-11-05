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

    public required string Tittel { get; set; }
    public required string Info { get; set; }

    // Hva slags hendelse det er - styrer farge og avkryssingen under "Vis i
    // kalender" på /kalender. Verdiene er nøklene i KalenderKategori.
    public string Kategori { get; set; } = "internt";

    // Satt når hendelsen er en del av en arbeidsordre. Slik kan en lang ordre
    // ligge på flere dager og flere personer, mens selve ordren bare har én
    // planlagt dato og én ansvarlig montør.
    public int? ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
