namespace PortalItlock.Web.Models;

public class ForesporselMedia
{
    public int Id { get; set; }
    public int ForesporselId { get; set; }
    public Foresporsel? Foresporsel { get; set; }
    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }

    // Innebygde bilder i e-posten (f.eks. logoen i signaturen) har en Content-ID som
    // HTML-en peker på med src="cid:...". De vises i e-posten, ikke som vedlegg.
    public string? ContentId { get; set; }
    public bool ErInline { get; set; }
}
