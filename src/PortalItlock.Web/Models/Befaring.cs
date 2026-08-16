namespace PortalItlock.Web.Models;

public class Befaring
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public string? Kundenr { get; set; }
    public string? Kundenavn { get; set; }
    public string? Bygg { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Tlf { get; set; }
    public string? Epost { get; set; }
    public DateTime? Dato { get; set; }
    public string? SystemNr { get; set; }
    public string? BefartAv { get; set; }
    public string? Oppdrag { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<BefaringDorfelt> Dorfelt { get; set; } = [];
}
