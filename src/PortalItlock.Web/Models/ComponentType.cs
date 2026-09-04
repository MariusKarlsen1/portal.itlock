namespace PortalItlock.Web.Models;

public class ComponentType
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public CeTilbehorKategori CeKategori { get; set; } = CeTilbehorKategori.Ingen;
    public byte[]? CeDokumentData { get; set; }
    public string? CeDokumentFilnavn { get; set; }
    public string? CeDokumentContentType { get; set; }

    public List<Component> Komponenter { get; set; } = [];
}
