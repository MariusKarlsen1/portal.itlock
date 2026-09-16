namespace PortalItlock.Web.Models;

// En vare kan ha flere datablad (utover hoveddokumentet på selve Component -
// DatabladData/DatabladFilnavn/DatabladContentType), samme mønster som
// ComponentFdvDokument og ComponentMontasjebladDokument.
public class ComponentDatabladDokument
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
