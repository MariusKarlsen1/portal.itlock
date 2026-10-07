namespace PortalItlock.Web.Models;

// Fast pris per lisens per måned, felles for alle organisasjoner - én rad
// per LisensType. Brukes til å beregne hver organisasjons månedlige beløp
// (AntallTildelt × KrPerManed, summert over lisenstypene) og plattformens
// samlede månedlige lisensinntekt.
public class LisensPris
{
    public int Id { get; set; }
    public LisensType Type { get; set; }
    public decimal KrPerManed { get; set; }
}
