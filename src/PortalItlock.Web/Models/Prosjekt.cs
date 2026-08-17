namespace PortalItlock.Web.Models;

public class Prosjekt
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public string? Kunde { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? System { get; set; }
    public ProsjektStatus? Status { get; set; }
    public string? Notater { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Plantegning> Plantegninger { get; set; } = [];
    public List<Dor> Dorer { get; set; } = [];
    public List<ProsjektVedlegg> Vedlegg { get; set; } = [];
    public List<Tilbud> Tilbud { get; set; } = [];
    public List<Arbeidsordre> Arbeidsordre { get; set; } = [];
}
