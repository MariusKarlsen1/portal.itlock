namespace PortalItlock.Web.Models;

public class Prosjekt
{
    public int Id { get; set; }
    public required string Navn { get; set; }

    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? System { get; set; }
    public string? Byggkategori { get; set; }
    public string? Risikoklasse { get; set; }
    public string? Tek { get; set; }
    public int CeGyldighetMåneder { get; set; } = 12;
    public ProsjektStatus? Status { get; set; }
    public ProsjektType Type { get; set; } = ProsjektType.Annet;
    public DateTime? OverlevertDato { get; set; }
    public string? Notater { get; set; }
    public string? Info { get; set; }
    public string? FdvForside { get; set; }
    public string? LasplanSystemnr { get; set; }
    public string? LasplanProsjektnummer { get; set; }
    public string? LasplanUtarbeidetAv { get; set; }
    public bool LasplanLast { get; set; } = true;

    // Godkjenning av låsplanen: sendes til kunden med en lenke (token), kunden
    // kan endre kryss og godkjenner med navn og signatur. Se LasplanGodkjenningService.
    public LasplanStatus LasplanStatus { get; set; }
    public string? LasplanSendtTil { get; set; }
    public DateTime? LasplanSendtDato { get; set; }
    public string? LasplanToken { get; set; }
    public DateTime? LasplanTokenUtloper { get; set; }
    public string? LasplanGodkjentAv { get; set; }
    public string? LasplanGodkjentFirma { get; set; }
    public DateTime? LasplanGodkjentDato { get; set; }
    /// Signaturen som PNG data-URL.
    public string? LasplanSignatur { get; set; }
    /// JSON: kryss kunden endret før godkjenning, vises gult i matrisen.
    public string? LasplanKundeEndringer { get; set; }
    /// JSON: historikk (tid + tekst).
    public string? LasplanLogg { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public int? ProsjektlederId { get; set; }
    public Bruker? Prosjektleder { get; set; }
    public int? AnsvarligMontorId { get; set; }
    public Bruker? AnsvarligMontor { get; set; }

    public List<Plantegning> Plantegninger { get; set; } = [];
    public List<Dor> Dorer { get; set; } = [];
    public List<ProsjektVedlegg> Vedlegg { get; set; } = [];
    public List<Tilbud> Tilbud { get; set; } = [];
    public List<MonteringLinje> MonteringLinjer { get; set; } = [];

    // Fristene i fremdriftsplanen (sjekklisten i økonomiarket). Fasene regnes
    // fra dem, se FremdriftsplanBeregner. Godkjent fristforlengelse på
    // endringsmeldingene legges til overleveringsfristen.
    public DateTime? StartDato { get; set; }
    public DateTime? MontasjestartDato { get; set; }
    public DateTime? OverleveringsfristDato { get; set; }

    // Fylles inn under Prosjektøkonomi: indeksregulering som legges til
    // prosjektsummen, resterende timer når forslaget fra fremdriften ikke
    // stemmer, og bankgaranti med påminnelse.
    public decimal? Indeksregulering { get; set; }
    public decimal? ResterendeTimer { get; set; }

    // Faktisk varekost lagt inn for hånd (for eksempel fra Tripletex) når
    // varene ikke tas ut fra lageret. Ellers anslås varene fra tilbudet.
    public decimal? FaktiskVarekost { get; set; }

    public decimal? BankgarantiBelop { get; set; }
    public DateTime? BankgarantiTil { get; set; }

    public decimal? ServiceTimepris { get; set; }

    // Hvor mange servicerunder avtalen skal ha i året (1 = årlig, 2 = halvårlig
    // osv.). Null = ikke satt; da leses intervallet fra rundene i stedet.
    public int? ServiceGangerPerAr { get; set; }

    // Sjekklisten servicerundene på avtalen får (se ServiceSjekkliste). Null =
    // standardlisten. Bevisst uten navigasjon/FK - slettes en liste, settes
    // feltet tilbake til null i koden (ServiceSjekklistemal.razor).
    public int? ServiceSjekklisteId { get; set; }
    public List<ServiceMinuttLinje> ServiceMinuttLinjer { get; set; } = [];
    public List<ServiceKategoriPris> ServiceKategoriPriser { get; set; } = [];
    public List<Arbeidsordre> Arbeidsordre { get; set; } = [];
    public List<DorIdMal> DorIdMaler { get; set; } = [];
    public List<Bruker> Medlemmer { get; set; } = [];
}
