namespace PortalItlock.Web.Models;

public class Arbeidsordre
{
    public int Id { get; set; }
    public required string Tittel { get; set; }
    public string? Beskrivelse { get; set; }

    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int? AnsvarligMontorId { get; set; }
    public Bruker? AnsvarligMontor { get; set; }

    public int? TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }

    public ArbeidsordreStatus Status { get; set; } = ArbeidsordreStatus.Ny;
    public DateTime? PlanlagtDato { get; set; }
    public DateTime? PlanlagtSlutt { get; set; }

    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Timeregistrering> Timeregistreringer { get; set; } = [];
    public List<ArbeidsordreSjekkpunkt> Sjekkpunkter { get; set; } = [];
}
