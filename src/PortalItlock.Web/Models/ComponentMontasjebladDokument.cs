namespace PortalItlock.Web.Models;

// En vare kan ha flere montasjeblad (utover hoveddokumentet på selve
// Component - MontasjebladData/MontasjebladFilnavn/MontasjebladContentType),
// samme mønster som ComponentFdvDokument.
public class ComponentMontasjebladDokument
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
