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
}
