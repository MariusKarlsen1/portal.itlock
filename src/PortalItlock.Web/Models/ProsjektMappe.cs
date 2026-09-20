namespace PortalItlock.Web.Models;

// Mappe for å organisere ProsjektVedlegg under prosjektets Vedlegg-fane -
// erstatter den gamle frittekst-"Type" på ProsjektVedlegg, se migrasjonen
// LeggTilProsjektMapper for datamigreringen av eksisterende verdier.
public class ProsjektMappe
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Navn { get; set; }
    public int Sortering { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<ProsjektVedlegg> Vedlegg { get; set; } = [];
}
