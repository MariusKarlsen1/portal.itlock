namespace PortalItlock.Web.Models;

// Hva slags innlegg en Kunngjoring er (oktober 2026). Kunngjoring er 0 slik at
// alle eldre kunngjøringer beholder sin betydning.
public enum InnleggType { Kunngjoring, Diskusjon, Sporsmal, Ros, Avstemning, Arrangement }

public static class InnleggTypeExtensions
{
    public static string Visningsnavn(this InnleggType type) => type switch
    {
        InnleggType.Kunngjoring => "Kunngjøring",
        InnleggType.Diskusjon => "Diskusjon",
        InnleggType.Sporsmal => "Spørsmål",
        InnleggType.Ros => "Ros",
        InnleggType.Avstemning => "Avstemning",
        InnleggType.Arrangement => "Arrangement",
        _ => type.ToString()
    };

    public static string Emoji(this InnleggType type) => type switch
    {
        InnleggType.Kunngjoring => "📣",
        InnleggType.Diskusjon => "💬",
        InnleggType.Sporsmal => "❓",
        InnleggType.Ros => "🏅",
        InnleggType.Avstemning => "📊",
        InnleggType.Arrangement => "📅",
        _ => "💬"
    };

    // Typer som vises med egen overskrift (de andre er korte innlegg uten tittel).
    public static bool HarTittel(this InnleggType type) =>
        type is InnleggType.Kunngjoring or InnleggType.Sporsmal or InnleggType.Avstemning or InnleggType.Arrangement;
}

// Merkene man kan gi i et Ros-innlegg.
public static class RosMerke
{
    public static readonly (string Id, string Emoji, string Navn)[] Alle =
    [
        ("godt", "🏅", "Godt jobba"),
        ("lag", "🤝", "Lagspiller"),
        ("rask", "⚡", "Rask levering"),
        ("kunde", "😊", "Fornøyd kunde"),
        ("ide", "💡", "God idé")
    ];

    public static (string Id, string Emoji, string Navn) Hent(string? id) =>
        Alle.FirstOrDefault(m => m.Id == id) is { Id: not null } m ? m : Alle[0];
}

// Fargebakgrunner for korte innlegg (CSS-klasser kg-bg-*).
public static class InnleggBakgrunn
{
    public static readonly string[] Alle = ["kobber", "skog", "hav", "lilla", "sol", "natt"];

    public static bool ErGyldig(string? id) => id is not null && Alle.Contains(id);
}

// Bekreftelse på "Lest og forstått" for kunngjøringer som krever det.
public class KunngjoringBekreftelse
{
    public int Id { get; set; }
    public int KunngjoringId { get; set; }
    public Kunngjoring? Kunngjoring { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
}

// Hvem som har sett et innlegg ("Sett av 29") - også grunnlaget for uleste.
public class KunngjoringVisning
{
    public int Id { get; set; }
    public int KunngjoringId { get; set; }
    public Kunngjoring? Kunngjoring { get; set; }
    public int BrukerId { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
}

public class AvstemningValg
{
    public int Id { get; set; }
    public int KunngjoringId { get; set; }
    public Kunngjoring? Kunngjoring { get; set; }
    public required string Tekst { get; set; }
    public int Rekkefolge { get; set; }
    public List<AvstemningStemme> Stemmer { get; set; } = [];
}

// Én stemme per bruker per avstemning (KunngjoringId er med for den unike indeksen).
public class AvstemningStemme
{
    public int Id { get; set; }
    public int ValgId { get; set; }
    public AvstemningValg? Valg { get; set; }
    public int KunngjoringId { get; set; }
    public int BrukerId { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
}
