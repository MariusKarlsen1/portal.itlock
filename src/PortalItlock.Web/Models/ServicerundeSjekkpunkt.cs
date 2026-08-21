namespace PortalItlock.Web.Models;

public class ServicerundeSjekkpunkt
{
    public int Id { get; set; }
    public int ServicerundeId { get; set; }
    public Servicerunde? Servicerunde { get; set; }

    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }
    public bool Fullfort { get; set; }
    public DateTime? FullfortDato { get; set; }

    public int? FullfortAvBrukerId { get; set; }
    public Bruker? FullfortAvBruker { get; set; }
}
