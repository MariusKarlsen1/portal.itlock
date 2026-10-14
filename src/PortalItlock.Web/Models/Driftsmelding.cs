namespace PortalItlock.Web.Models;

public class Driftsmelding
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public required string Tekst { get; set; }
    public string? Tittel { get; set; }

    // Hva meldingen gjelder, valgt med ett trykk i driftsportalen (se DriftHelper.Kategorier).
    public string? Kategori { get; set; }

    // Vaktmesteren har krysset av for at det haster (døren står åpen, sikkerhet o.l.).
    public bool Haster { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public int? InnmeldtAvBrukerId { get; set; }
    public Bruker? InnmeldtAvBruker { get; set; }

    // Satt til true når en admin/prosjektleder har åpnet meldingen, brukes til å fjerne den fra varselbjellen.
    public bool LestAvAnsatt { get; set; }

    // Når meldingen ble åpnet første gang - vises som "Lest" i tidslinjen hos vaktmesteren.
    public DateTime? LestDato { get; set; }

    public List<DriftsmeldingMedia> Media { get; set; } = [];
}
