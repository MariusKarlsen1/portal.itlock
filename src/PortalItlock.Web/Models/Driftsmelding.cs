namespace PortalItlock.Web.Models;

public class Driftsmelding
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public required string Tekst { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public int? InnmeldtAvBrukerId { get; set; }
    public Bruker? InnmeldtAvBruker { get; set; }

    // Satt til true når en admin/prosjektleder har åpnet meldingen, brukes til å fjerne den fra varselbjellen.
    public bool LestAvAnsatt { get; set; }

    public List<DriftsmeldingMedia> Media { get; set; } = [];
}
