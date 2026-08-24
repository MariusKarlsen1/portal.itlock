namespace PortalItlock.Web.Models;

public class TilbudRevisjon
{
    public int Id { get; set; }
    public int TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }

    public int Versjonsnummer { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public required string Tittel { get; set; }
    public TilbudPrisType PrisType { get; set; }
    public decimal Prosentsats { get; set; }
    public decimal Timepris { get; set; }
    public decimal? Montasjekost { get; set; }

    public required string LinjerJson { get; set; }
}

public class TilbudLinjeSnapshot
{
    public int LinjeId { get; set; }
    public string Navn { get; set; } = "";
    public decimal Innpris { get; set; }
    public decimal Utpris { get; set; }
    public int Antall { get; set; }
    public string? Enhet { get; set; }
    public LevertAv LevertAv { get; set; }
    public decimal RabattProsent { get; set; }
}
