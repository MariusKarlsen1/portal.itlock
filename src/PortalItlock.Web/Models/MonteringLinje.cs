namespace PortalItlock.Web.Models;

public class MonteringLinje
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int? ComponentId { get; set; }
    public Component? Component { get; set; }

    public required string Navn { get; set; }
    public int Antall { get; set; } = 1;
    public string? Enhet { get; set; }
    public int? Minutter { get; set; }
    public int Rekkefolge { get; set; }

    // Varer som bare er opsjon på dørene. Vises i egen seksjon under Montering og
    // regnes ikke med i prosjektets kalkulerte tid.
    public bool ErOpsjon { get; set; }
}
