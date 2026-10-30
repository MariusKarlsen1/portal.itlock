namespace PortalItlock.Web.Models;

// Kassa (tilleggsmodul «kassa»): kontantsalg i butikk der beløpet slås inn
// manuelt på bankterminalen. Hvert salg får en nummerert kontantfaktura
// (bokføringsforskriften § 5-4-1 tredje ledd), så salget ikke må registreres
// i et produkterklært kassasystem. Salg slettes eller endres aldri - feil
// rettes med kreditnota, så nummerserien er uten hull. Se KassaService.

// Knappene i kassa. Kan kobles til en vare i komponentregisteret: da brukes
// varens bilde, og salg trekkes fra lageret.
public class KasseVare
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string Kategori { get; set; } = "Annet";
    public decimal PrisInklMva { get; set; }
    public decimal MvaSats { get; set; } = 25;
    public int? ComponentId { get; set; }
    public Component? Component { get; set; }
    public string? Strekkode { get; set; }
    // Symbol når varen ikke har bilde (se KassaIkon).
    public string Ikon { get; set; } = "boks";
    public bool Favoritt { get; set; }
    public int Rekkefolge { get; set; }
    public bool Aktiv { get; set; } = true;
}

public enum KasseSalgType
{
    Kort,
    Kontant,
    Kreditnota
}

public class KasseSalg
{
    public int Id { get; set; }
    // Løpenummer for kontantfaktura og kreditnota (én felles serie, uten hull).
    public int Nummer { get; set; }
    public DateTime Tidspunkt { get; set; }
    public KasseSalgType Type { get; set; }
    // For kreditnota: hvordan pengene ble betalt tilbake (Kort eller Kontant).
    public KasseSalgType? BetaltTilbakeMed { get; set; }
    public int? KrediterSalgId { get; set; }
    public KasseSalg? KrediterSalg { get; set; }
    public string? Arsak { get; set; }
    public string? KundeNavn { get; set; }
    public string? KundeEpost { get; set; }
    public decimal RabattProsent { get; set; }
    // Inkl. mva. Negativt for kreditnota.
    public decimal Total { get; set; }
    public decimal Mva { get; set; }
    public decimal? Mottatt { get; set; }
    public int? BrukerId { get; set; }
    public string? BrukerNavn { get; set; }
    public string? KasseNavn { get; set; }
    // Settes når dagen lukkes. Salg uten oppgjør hører til dagen som er åpen.
    public int? DagsoppgjorId { get; set; }
    public KasseDagsoppgjor? Dagsoppgjor { get; set; }
    public List<KasseSalgLinje> Linjer { get; set; } = [];
}

public class KasseSalgLinje
{
    public int Id { get; set; }
    public int KasseSalgId { get; set; }
    public KasseSalg? KasseSalg { get; set; }
    public int? KasseVareId { get; set; }
    public int? ComponentId { get; set; }
    public required string Navn { get; set; }
    public string? Kategori { get; set; }
    // Negativt for kreditnota.
    public int Antall { get; set; }
    public decimal Enhetspris { get; set; }
    public decimal MvaSats { get; set; }
    public decimal RabattProsent { get; set; }
    // Inkl. mva, etter rabatt.
    public decimal Belop { get; set; }
    public decimal Mva { get; set; }
}

// Lukket dag: terminaltotal og opptelling mot det som er registrert. Kan
// ikke endres etter at den er lukket, bortsett fra bankavstemmingen.
public class KasseDagsoppgjor
{
    public int Id { get; set; }
    public DateTime Lukket { get; set; }
    public string? LukketAv { get; set; }
    public string? KasseNavn { get; set; }
    public int? FraNummer { get; set; }
    public int? TilNummer { get; set; }
    public int AntallSalg { get; set; }
    public int AntallKreditnota { get; set; }
    public decimal Omsetning { get; set; }
    public decimal Mva { get; set; }
    public decimal KortIKassa { get; set; }
    public decimal KontantIKassa { get; set; }
    public decimal TerminalTotal { get; set; }
    public decimal VekselStart { get; set; }
    public decimal Opptalt { get; set; }
    public string? TellingJson { get; set; }
    public decimal DifferanseKort { get; set; }
    public decimal DifferanseKontant { get; set; }
    public string? Forklaring { get; set; }
    // Bankavstemming: kort utbetalt fra innløser og kontanter satt inn i banken.
    public decimal? UtbetaltBank { get; set; }
    public decimal? SattInnBank { get; set; }
    public List<KasseSalg> Salg { get; set; } = [];

    public decimal TilBank => Math.Max(0, Opptalt - VekselStart);
}

// Oppsett for kassa (én rad).
public class KasseOppsett
{
    public int Id { get; set; }
    public string KasseNavn { get; set; } = "Kasse 1";
    public decimal Vekselbeholdning { get; set; } = 2000;
    public bool MvaRegistrert { get; set; } = true;
    public string? Bunntekst { get; set; } = "Takk for handelen!";
}
