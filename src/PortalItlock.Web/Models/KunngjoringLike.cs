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
    public const string Ler = "ler";
    public const string Wow = "wow";
    public const string Takk = "takk";

    public static readonly (string Id, string Emoji, string Navn)[] Alle =
    [
        (Tommel, "👍", "Liker"),
        (Hjerte, "❤️", "Elsker"),
        (Feiring, "🎉", "Gratulerer"),
        (Ler, "😂", "Morsomt"),
        (Wow, "😮", "Wow"),
        (Takk, "🙏", "Takk")
    ];

    public static (string Id, string Emoji, string Navn) Hent(string? id) =>
        Alle.FirstOrDefault(r => r.Id == id) is { Id: not null } r ? r : Alle[0];
}
