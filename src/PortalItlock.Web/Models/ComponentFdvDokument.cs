namespace PortalItlock.Web.Models;

// En vare kan ha flere FDV-dokumenter (utover hoveddokumentet på selve
// Component - FdvData/FdvFilnavn/FdvContentType) - f.eks. eget bruksanvisning
// + eget samsvarserklæring for samme vare.
public class ComponentFdvDokument
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
