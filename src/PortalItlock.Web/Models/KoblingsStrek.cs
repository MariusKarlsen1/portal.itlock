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

    // Flerlederkabel: tegnes som en tykk sort kabel som deler seg i fargede
    // ledere i hver ende. Null = vanlig strek. Se KoblingsKabel.
    public string? LedereJson { get; set; }

    // Kabelnavn som vises på tegningen og i kabellisten, f.eks. "K1".
    public string? Navn { get; set; }

    // Kabelbetegnelse, f.eks. "6 PAR 0,6 PT HF (maks 100 m)".
    public string? KabelType { get; set; }

    public bool ErKabel => LedereJson is not null;
}

public class KoblingsStrekPunkt
{
    public double X { get; set; }
    public double Y { get; set; }
}
