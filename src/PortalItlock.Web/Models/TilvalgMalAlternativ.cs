namespace PortalItlock.Web.Models;

public class TilvalgMalAlternativ
{
    public int Id { get; set; }
    public int TilvalgMalId { get; set; }
    public TilvalgMal? TilvalgMal { get; set; }

    public required string Navn { get; set; }
    public string? Utforelse { get; set; }
    public decimal Pris { get; set; }
    public int Rekkefolge { get; set; }

    public byte[]? BildeData { get; set; }
    public string? BildeContentType { get; set; }
}
