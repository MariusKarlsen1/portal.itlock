namespace PortalItlock.Web.Models;

// En sjekkliste for servicerunder (f.eks. «Standard», «Automatikkdører»).
// Serviceavtalen velger liste (Prosjekt.ServiceSjekklisteId); har den ikke
// valgt noen, brukes listen som er merket ErStandard. Punktene kopieres inn i
// servicerunden når den registreres, så endringer her gjelder nye runder.
public class ServiceSjekkliste
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public bool ErStandard { get; set; }
    public List<ServicerundeSjekklistepunkt> Punkter { get; set; } = [];
}