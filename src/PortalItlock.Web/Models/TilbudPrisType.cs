namespace PortalItlock.Web.Models;

public enum TilbudPrisType { Dekningsgrad, Paslag }

public static class TilbudPrisTypeExtensions
{
    public static string Visningsnavn(this TilbudPrisType type) => type switch
    {
        TilbudPrisType.Dekningsgrad => "DG på varer",
        TilbudPrisType.Paslag => "Påslag",
        _ => type.ToString()
    };
}
