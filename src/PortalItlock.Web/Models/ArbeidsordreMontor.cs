namespace PortalItlock.Web.Models;

// Flere montører på samme arbeidsordre, i tillegg til den ansvarlige
// (Arbeidsordre.AnsvarligMontorId). Ordren vises for alle i montørappen og kalenderen.
public class ArbeidsordreMontor
{
    public int Id { get; set; }
    public int ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
}
