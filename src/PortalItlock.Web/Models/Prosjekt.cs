namespace PortalItlock.Web.Models;

public class Prosjekt
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? System { get; set; }
    public string? Byggkategori { get; set; }
    public string? Risikoklasse { get; set; }
    public int CeGyldighetMåneder { get; set; } = 12;
    public ProsjektStatus? Status { get; set; }
    public string? Notater { get; set; }
    public string? Info { get; set; }
    public string? FdvForside { get; set; }
    public string? LasplanSystemnr { get; set; }
    public string? LasplanProsjektnummer { get; set; }
    public string? LasplanUtarbeidetAv { get; set; }
    public bool LasplanLast { get; set; } = true;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Plantegning> Plantegninger { get; set; } = [];
    public List<Dor> Dorer { get; set; } = [];
    public List<ProsjektVedlegg> Vedlegg { get; set; } = [];
    public List<Tilbud> Tilbud { get; set; } = [];
    public List<Arbeidsordre> Arbeidsordre { get; set; } = [];
    public List<DorIdMal> DorIdMaler { get; set; } = [];
    public List<Bruker> Medlemmer { get; set; } = [];
}
