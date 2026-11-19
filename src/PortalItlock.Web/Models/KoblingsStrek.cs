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

    // Kabelnavnet vises ikke på tegningen.
    public bool SkjulNavn { get; set; }

    // Tekst rett over streken ved enden den er koblet til, f.eks. «+12V» eller «NO».
    public string? TekstStart { get; set; }
    public string? TekstSlutt { get; set; }

    // Merkingen står på enden av streken (i forlengelsen) i stedet for over den.
    public bool TekstVedEnden { get; set; }

    // Egen farge på merkingen (null = samme som streken).
    public string? TekstFarge { get; set; }

    // Kabelnavnet (K1 · 8-leder): null = midt på, "start" eller "slutt" = ved den enden.
    public string? KabelMerkePlass { get; set; }

    // Kabelbetegnelse, f.eks. "6 PAR 0,6 PT HF (maks 100 m)".
    public string? KabelType { get; set; }

    // Avstand mellom lederne der kabelen deler seg: "Tett", "Bred" eller null (normal).
    public string? LederAvstand { get; set; }

    // Hvor langt de løse lederne stikker ut av kabelen, som faktor (1 = standard).
    // Null = standard.
    public double? LederLengde { get; set; }

    public bool ErKabel => LedereJson is not null;
}

public class KoblingsStrekPunkt
{
    public double X { get; set; }
    public double Y { get; set; }
}
