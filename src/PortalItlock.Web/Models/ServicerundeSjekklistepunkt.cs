namespace PortalItlock.Web.Models;

public class ServicerundeSjekklistepunkt
{
    public int Id { get; set; }
    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }

    // Hvilken sjekkliste punktet hører til (se ServiceSjekkliste). Punkter fra
    // før det fantes flere lister ble flyttet til «Standard» av migreringen.
    public int? ServiceSjekklisteId { get; set; }
    public ServiceSjekkliste? ServiceSjekkliste { get; set; }
}