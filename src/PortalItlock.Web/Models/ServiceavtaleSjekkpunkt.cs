namespace PortalItlock.Web.Models;

// Faste sjekkpunkter for én serviceavtale (prosjekt med status Serviceavtale).
// Kommer i tillegg til den felles sjekklistemalen (ServicerundeSjekklistepunkt)
// og legges ved hver servicerunde på avtalen.
public class ServiceavtaleSjekkpunkt
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }
    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }
}
