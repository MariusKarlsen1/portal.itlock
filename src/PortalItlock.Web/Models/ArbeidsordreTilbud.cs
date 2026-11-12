namespace PortalItlock.Web.Models;

// Ekstra tilbud knyttet til en arbeidsordre. Det første tilbudet ligger i
// Arbeidsordre.TilbudId (kunde, fastpris m.m. følger det); resten ligger her.
// Varene fra alle tilbudene vises og plukkes på arbeidsordren.
// Se ArbeidsordreTilbudHelper.
public class ArbeidsordreTilbud
{
    public int Id { get; set; }
    public int ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }
    public int TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }
    public DateTime LagtTilDato { get; set; } = DateTime.Now;
}
