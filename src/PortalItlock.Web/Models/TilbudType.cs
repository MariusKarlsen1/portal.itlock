namespace PortalItlock.Web.Models;

public enum TilbudType { Tilbud, Endringsmelding }

public static class TilbudTypeExtensions
{
    public static string Visningsnavn(this TilbudType type) => type switch
    {
        TilbudType.Tilbud => "Tilbud",
        TilbudType.Endringsmelding => "Endringsmelding/tillegg",
        _ => type.ToString()
    };
}
