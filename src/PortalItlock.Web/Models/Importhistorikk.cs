namespace PortalItlock.Web.Models;

public class Importhistorikk
{
    public int Id { get; set; }
    public required string Kilde { get; set; }
    public string? Filnavn { get; set; }
    public string? Leverandor { get; set; }

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public string Status { get; set; } = "Pågår";
    public int? AntallRader { get; set; }
    public int? AntallNye { get; set; }
    public int? AntallOppdatert { get; set; }

    // Kolonnevalgene (PrisimportService.KolonneForslag) som JSON, lagret når
    // importen fullføres - lar en senere import for samme leverandør
    // gjenbruke nøyaktig de samme feltvalgene i stedet for å måtte mappes på
    // nytt fra bunnen av (se Prisimport.razor).
    public string? MappingJson { get; set; }

    // Peker til den ALLERFØRSTE importen av en fil med akkurat samme
    // filnavn, satt automatisk når filnavnet matcher en tidligere import
    // - lar historikken vise senere kjøringer som "Oppdatering" under den
    // opprinnelige, i stedet for som løsrevne rader (se Prisimport.razor).
    public int? OpprinneligImportId { get; set; }
    public Importhistorikk? OpprinneligImport { get; set; }
}
