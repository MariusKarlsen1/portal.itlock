namespace PortalItlock.Web.Models;

public class KoblingsStrek
{
    public int Id { get; set; }
    public int KoblingsSkjemaId { get; set; }
    public KoblingsSkjema? KoblingsSkjema { get; set; }

    // Serialisert liste av punkter (i prosent av lerretet) som utgjør streken, f.eks. [{"X":10,"Y":20},{"X":30,"Y":40}].
    public required string PunkterJson { get; set; }

    public string Farge { get; set; } = "#835e41";
    public int Tykkelse { get; set; } = 2;
    public bool Stiplet { get; set; }
}

public class KoblingsStrekPunkt
{
    public double X { get; set; }
    public double Y { get; set; }
}
