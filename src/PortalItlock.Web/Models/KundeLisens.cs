namespace PortalItlock.Web.Models;

// En lisens (f.eks. et adgangskontroll- eller nøkkelsystem-abonnement) knyttet
// til en kunde, med utløpsdato slik at Årshjulet på kundekortet kan vise når på
// året den må fornyes - se LisensVarselService for e-postvarselet 1 måned før.
public class KundeLisens
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public required string Navn { get; set; }
    public DateTime UtlopsDato { get; set; }
    public string? Notat { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
