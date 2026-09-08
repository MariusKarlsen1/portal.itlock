namespace PortalItlock.Web.Models;

public class KundeRabatt
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }
    public int ProduktgruppeId { get; set; }
    public Produktgruppe? Produktgruppe { get; set; }
    public decimal RabattProsent { get; set; }
}
