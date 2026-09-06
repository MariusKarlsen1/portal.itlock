namespace PortalItlock.Web.Models;

public enum TilbudPrisType { Dekningsgrad, Paslag, Veiledende, Rabatt }

public static class TilbudPrisTypeExtensions
{
    public static string Visningsnavn(this TilbudPrisType type) => type switch
    {
        TilbudPrisType.Dekningsgrad => "DG på varer",
        TilbudPrisType.Paslag => "Påslag",
        TilbudPrisType.Veiledende => "Veiledende",
        TilbudPrisType.Rabatt => "Rabatt på veiledende",
        _ => type.ToString()
    };
}
