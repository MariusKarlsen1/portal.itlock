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

    public List<ForesporselMedia> Media { get; set; } = [];
    public List<ForesporselNotat> Notater { get; set; } = [];
}
