namespace PortalItlock.Web.Models;

// Lenke til filer eller mapper som ligger andre steder (OneDrive, SharePoint, Dalux,
// Google Drive o.l.), vist under Vedlegg på prosjektet.
public class ProsjektLenke
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Navn { get; set; }
    public required string Url { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
}
