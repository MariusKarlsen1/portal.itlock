namespace PortalItlock.Web.Models;

public class ArbeidsordreSjekkpunkt
{
    public int Id { get; set; }
    public int ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }
    public bool Fullfort { get; set; }
    public DateTime? FullfortDato { get; set; }

    public int? FullfortAvBrukerId { get; set; }
    public Bruker? FullfortAvBruker { get; set; }
}
