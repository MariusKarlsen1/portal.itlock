namespace PortalItlock.Web.Models;

public class Nokkelsystem
{
    public int Id { get; set; }
    public required string Systemnummer { get; set; }

    public string? Kundenavn { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Fabrikat { get; set; }
    public string? Notater { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Rekvirent> Rekvirenter { get; set; } = [];
}
