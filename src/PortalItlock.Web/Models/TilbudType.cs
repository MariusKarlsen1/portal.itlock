namespace PortalItlock.Web.Models;

public enum TilbudType { Tilbud, Endringsmelding, Opsjon }

public static class TilbudTypeExtensions
{
    public static string Visningsnavn(this TilbudType type) => type switch
    {
        TilbudType.Tilbud => "Tilbud",
        TilbudType.Endringsmelding => "Endringsmelding/tillegg",
        TilbudType.Opsjon => "Opsjoner",
        _ => type.ToString()
    };
}
