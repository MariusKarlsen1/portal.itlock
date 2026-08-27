namespace PortalItlock.Web.Models;

public class TilvalgAlternativ
{
    public int Id { get; set; }
    public int TilvalgId { get; set; }
    public Tilvalg? Tilvalg { get; set; }

    public required string Navn { get; set; }
    public string? Utforelse { get; set; }
    public decimal Pris { get; set; }
    public int Rekkefolge { get; set; }

    public byte[]? BildeData { get; set; }
    public string? BildeContentType { get; set; }

    // Antall kunden valgte av dette alternativet. 0 = ikke valgt. Satt når tilvalget besvares.
    public int ValgtAntall { get; set; }
}
