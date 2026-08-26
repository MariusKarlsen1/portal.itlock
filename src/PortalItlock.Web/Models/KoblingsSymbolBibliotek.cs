namespace PortalItlock.Web.Models;

// Et gjenbrukbart symbolbilde, lastet opp én gang og tilgjengelig for alle koblingsskjema uansett kategori.
public class KoblingsSymbolBibliotek
{
    public int Id { get; set; }
    public string? Navn { get; set; }
    public required byte[] BildeData { get; set; }
    public required string BildeContentType { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
