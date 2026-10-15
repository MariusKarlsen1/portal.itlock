namespace PortalItlock.Web.Models;

public class PlukklisteLinje
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int AntallPlukket { get; set; }
    public int VarerBestilt { get; set; }

    // Når bestilte varer ventes inn - plukklisten varsler hvis det er etter
    // første planlagte montasje på prosjektet.
    public DateTime? ForventetLevert { get; set; }
}
