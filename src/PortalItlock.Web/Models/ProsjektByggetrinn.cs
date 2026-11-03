namespace PortalItlock.Web.Models;

// Byggetrinnene i et prosjekt (grupperingen av plantegninger). Tegningene er
// koblet med navnet (Plantegning.Byggetrinn), som før; tabellen gjør at et
// byggetrinn kan finnes uten tegninger og har en rekkefølge.
public class ProsjektByggetrinn
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }
    public required string Navn { get; set; }
    public int Rekkefolge { get; set; }
}
