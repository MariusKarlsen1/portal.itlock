namespace PortalItlock.Web.Models;

public class HjemmesideTekst
{
    public int Id { get; set; }

    public required string Tittel { get; set; }
    public required string Ingress { get; set; }

    public required string MontorerTittel { get; set; }
    public required string MontorerBeskrivelse { get; set; }

    public required string ProsjektlederTittel { get; set; }
    public required string ProsjektlederBeskrivelse { get; set; }

    public required string AdminTittel { get; set; }
    public required string AdminBeskrivelse { get; set; }
}
