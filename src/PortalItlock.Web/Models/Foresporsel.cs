namespace PortalItlock.Web.Models;

public class Foresporsel
{
    public int Id { get; set; }
    public string FraEpost { get; set; } = "";
    public string? FraNavn { get; set; }
    public string Emne { get; set; } = "";
    public string Innhold { get; set; } = "";

    // E-postens HTML-versjon (med logo i signaturen, lenker og formatering). Vises i
    // en sandkasse-iframe i lesepanelet; Innhold (ren tekst) brukes til søk og forhåndsvisning.
    public string? InnholdHtml { get; set; }
    public DateTime MottattDato { get; set; } = DateTime.Now;
    public bool Lest { get; set; }
    public string RawJson { get; set; } = "";
    public ForesporselKanal Kanal { get; set; } = ForesporselKanal.Epost;
    // Satt når forespørselen er laget fra en driftsmelding (vaktmester i driftsportalen).
    public int? DriftsmeldingId { get; set; }

    // Saksbehandling rett på forespørselen (tidligere på en skjult ticket).
    public ForesporselStatus Status { get; set; } = ForesporselStatus.Ny;
    public int? AnsvarligBrukerId { get; set; }
    public Bruker? AnsvarligBruker { get; set; }
    public ForesporselPrioritet Prioritet { get; set; } = ForesporselPrioritet.Normal;
    public DateTime? Frist { get; set; }

    // Kunden forespørselen gjelder. Settes automatisk fra e-post eller domene
    // når den kommer inn, og kan endres i lesepanelet.
    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    // Satt når e-posten var videresendt (f.eks. fra Outlook): den som videresendte.
    // FraEpost/FraNavn er da den opprinnelige avsenderen.
    public string? VideresendtAv { get; set; }

    // Fra nettskjemaet.
    public string? Telefon { get; set; }
    public string? Adresse { get; set; }
    public string? Kategori { get; set; }

    // Arbeidsordre eller tilbud som er laget fra forespørselen.
    public int? ArbeidsordreId { get; set; }
    public int? TilbudId { get; set; }

    public List<ForesporselMedia> Media { get; set; } = [];
    public List<ForesporselNotat> Notater { get; set; } = [];
}
