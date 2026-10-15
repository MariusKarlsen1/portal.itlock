namespace PortalItlock.Web.Models;

// Fellesskap (oktober 2026, etter Viva Engage): kunngjøringer og samtaler
// organiseres i fellesskap som "Hele firmaet", "Montører", "HMS og rutiner".
// Obligatoriske fellesskap har alle interne brukere som medlemmer automatisk
// og kan ikke forlates. Hver organisasjon har sin egen database, så
// fellesskapene er alltid per organisasjon.
public class Fellesskap
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }

    // Farge på logo/omslag (hex) og omslagstema (emoji-sett, se FellesskapTema).
    public string Farge { get; set; } = "#8f7bd6";
    public string Tema { get; set; } = FellesskapTema.Alle;

    public bool ErObligatorisk { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }

    // Valgfritt eget omslagsbilde i stedet for tema-illustrasjonen.
    public byte[]? OmslagData { get; set; }
    public string? OmslagContentType { get; set; }

    public List<FellesskapMedlem> Medlemmer { get; set; } = [];
    public List<Kunngjoring> Innlegg { get; set; } = [];
}

// Medlemskap. For obligatoriske fellesskap trengs ingen rad for å være
// medlem - raden finnes da bare for favoritt/administrator.
public class FellesskapMedlem
{
    public int Id { get; set; }
    public int FellesskapId { get; set; }
    public Fellesskap? Fellesskap { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public bool ErAdmin { get; set; }
    public bool ErFavoritt { get; set; }
    public DateTime BleMedDato { get; set; } = DateTime.Now;
}

public static class FellesskapTema
{
    public const string Alle = "alle";

    // Id, navn, emoji på omslaget, gradient (fra, til).
    public static readonly (string Id, string Navn, string[] Emoji, string Fra, string Til)[] Liste =
    [
        ("alle", "Hele firmaet", ["📣", "🏢", "💬", "❤️"], "#b9a4e8", "#8f7bd6"),
        ("montor", "Montasje", ["🛠️", "🔩", "🚐", "📏"], "#8fd3c1", "#4fb39b"),
        ("service", "Service", ["🔧", "🔑", "🚪", "✅"], "#f2c48d", "#df9b52"),
        ("salg", "Salg", ["📈", "🤝", "📄", "💡"], "#9cc3ef", "#5f93d4"),
        ("hms", "HMS", ["🦺", "📋", "⚠️", "🧯"], "#f0a3c9", "#d468a3"),
        ("produkt", "Produkter", ["🔐", "📱", "🔋", "🆕"], "#c9b79a", "#8f6b48"),
        ("sosialt", "Sosialt", ["🎉", "🍕", "⛰️", "📸"], "#f6d36b", "#e8a93a")
    ];

    public static (string Id, string Navn, string[] Emoji, string Fra, string Til) Hent(string? id) =>
        Liste.FirstOrDefault(t => t.Id == id) is { Id: not null } t ? t : Liste[0];
}
