namespace PortalItlock.Web.Models;

public class Avvik
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public required string Beskrivelse { get; set; }
    public string? UtbedringBeskrivelse { get; set; }
    public decimal? Pris { get; set; }
    public AvvikStatus Status { get; set; } = AvvikStatus.Apent;

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }

    public DateTime? SendtTilKundeDato { get; set; }

    public byte[]? Signatur { get; set; }
    public string? SignertAvNavn { get; set; }
    public DateTime? SignertDato { get; set; }
}
