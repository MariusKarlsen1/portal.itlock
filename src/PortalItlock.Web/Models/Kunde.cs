namespace PortalItlock.Web.Models;

public class Kunde
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Notater { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Prosjekt> Prosjekter { get; set; } = [];
}
