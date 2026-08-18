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

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Timeregistrering> Timeregistreringer { get; set; } = [];
}
