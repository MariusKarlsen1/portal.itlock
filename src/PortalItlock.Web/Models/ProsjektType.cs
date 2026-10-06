namespace PortalItlock.Web.Models;

public enum ProsjektType { Nybygg, Rehabilitering, Automatikk, Service, Annet }

public static class ProsjektTypeExtensions
{
    public static string Visningsnavn(this ProsjektType type) => type switch
    {
        ProsjektType.Nybygg => "Nybygg",
        ProsjektType.Rehabilitering => "Rehabilitering",
        ProsjektType.Automatikk => "Automatikk",
        ProsjektType.Service => "Service",
        ProsjektType.Annet => "Annet",
        _ => type.ToString()
    };

    public static string Farge(this ProsjektType type) => type switch
    {
        ProsjektType.Nybygg => "#835e41",
        ProsjektType.Rehabilitering => "#2054a3",
        ProsjektType.Automatikk => "#7a2b96",
        ProsjektType.Service => "#2b7a4b",
        ProsjektType.Annet => "#9a958c",
        _ => "#9a958c"
    };
}
