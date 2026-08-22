namespace PortalItlock.Web.Models;

public class Servicerunde
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public DateTime Dato { get; set; } = DateTime.Now;

    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }

    public required string StatusBeskrivelse { get; set; }
    public string? Anbefalinger { get; set; }
    public DateTime? NesteServiceDato { get; set; }
    public string? Forside { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<ServicerundeDel> Deler { get; set; } = [];
    public List<ServicerundeSjekkpunkt> Sjekkpunkter { get; set; } = [];
    public List<ServicerundeMedia> Media { get; set; } = [];
}
