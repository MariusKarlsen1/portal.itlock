namespace PortalItlock.Web.Models;

// Én rad i Habo sin overgangstabell for utskiftning av gammel låskasse - se
// Services/LasOvergangData.cs for selve dataene.
public class LasOvergangRad
{
    public required string DuHarLas { get; init; }
    public required string Kategori { get; init; }
    public string? Laskasse { get; init; }
    public string? Utskiftningsskilt { get; init; }
    public string? Sylinder { get; init; }
    public string? Annet { get; init; }
    public string? Alternativt { get; init; }
    public string? Merknad { get; init; }
}
