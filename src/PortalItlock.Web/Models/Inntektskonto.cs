namespace PortalItlock.Web.Models;

// Portalens eget kontoregister (tilsvarer Tripletex sin kontoplan, men
// opprettes og vedlikeholdes her - se /rapporter/kontoplan). Kobles til en
// vare (Component.InntektskontoId) og brukes til å gruppere Resultatrapporten.
public class Inntektskonto
{
    public int Id { get; set; }
    public required int Nummer { get; set; }
    public required string Navn { get; set; }
}
