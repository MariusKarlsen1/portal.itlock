namespace PortalItlock.Web.Models;

public class KundeKontaktperson
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public required string Navn { get; set; }
    public string? Rolle { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
}
