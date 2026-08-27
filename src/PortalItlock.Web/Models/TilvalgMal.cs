namespace PortalItlock.Web.Models;

public class TilvalgMal
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public List<TilvalgMalAlternativ> Alternativer { get; set; } = [];
}
