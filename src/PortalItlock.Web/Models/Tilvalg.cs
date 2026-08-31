namespace PortalItlock.Web.Models;

public class Tilvalg
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Tittel { get; set; }
    public string? Beskrivelse { get; set; }
    public TilvalgStatus Status { get; set; } = TilvalgStatus.Utkast;

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public DateTime? PublisertDato { get; set; }
    public DateTime? BesvartDato { get; set; }

    // Satt til true når en admin/prosjektleder har åpnet et besvart tilvalg, brukes til å fjerne det fra varselbjellen.
    public bool LestAvAnsatt { get; set; }

    public byte[]? Signatur { get; set; }
    public string? SignertAvNavn { get; set; }
    public decimal? SumTotal { get; set; }

    // Kundens eget fritekst-ønske/referansebilde, i tillegg til de ferdige alternativene.
    public string? KundeOnskeTekst { get; set; }
    public byte[]? KundeOnskeBildeData { get; set; }
    public string? KundeOnskeBildeContentType { get; set; }

    public List<TilvalgAlternativ> Alternativer { get; set; } = [];
}
