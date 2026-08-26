namespace PortalItlock.Web.Models;

public class KoblingsSymbol
{
    public int Id { get; set; }
    public int KoblingsSkjemaId { get; set; }
    public KoblingsSkjema? KoblingsSkjema { get; set; }

    public KoblingsElementType ElementType { get; set; } = KoblingsElementType.Bilde;
    public string? Navn { get; set; }

    // Bilde - peker til et gjenbrukbart symbol i biblioteket, slik opplastede bilder er tilgjengelige i senere skjema også.
    public int? SymbolBibliotekId { get; set; }
    public KoblingsSymbolBibliotek? SymbolBibliotek { get; set; }

    // Rektangel/Sirkel/Linje/Pil
    public string Farge { get; set; } = "#835e41";
    public int Strokbredde { get; set; } = 2;
    public bool Fylt { get; set; }

    // Tekstboks
    public string? Tekst { get; set; }
    public int FontStorrelse { get; set; } = 14;

    // Posisjon og størrelse i prosent av lerretet, slik at det skalerer uavhengig av skjermstørrelse.
    public double PosX { get; set; } = 45;
    public double PosY { get; set; } = 45;
    public double Bredde { get; set; } = 10;
    public double Hoyde { get; set; } = 10;

    // Styrer rekkefølgen symbolene tegnes i (høyere verdi = lenger fremme).
    public int ZIndex { get; set; }

    // Låst mot flytting/endring av størrelse med mus, men fortsatt redigerbar via sidepanelet.
    public bool ErLaast { get; set; }
}
