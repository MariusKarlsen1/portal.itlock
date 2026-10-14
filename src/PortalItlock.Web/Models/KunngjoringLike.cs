namespace PortalItlock.Web.Models;

public class KunngjoringLike
{
    public int Id { get; set; }
    public int KunngjoringId { get; set; }
    public Kunngjoring? Kunngjoring { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    // Hvilken reaksjon brukeren ga (én per bruker): "tommel" (👍, standard -
    // også alle eldre likes), "feiring" (🎉) eller "hjerte" (❤️).
    public string Reaksjon { get; set; } = KunngjoringReaksjon.Tommel;
}

public static class KunngjoringReaksjon
{
    public const string Tommel = "tommel";
    public const string Feiring = "feiring";
    public const string Hjerte = "hjerte";

    public static readonly (string Id, string Emoji, string Navn)[] Alle =
    [
        (Tommel, "👍", "Liker"),
        (Feiring, "🎉", "Gratulerer"),
        (Hjerte, "❤️", "Elsker")
    ];
}
