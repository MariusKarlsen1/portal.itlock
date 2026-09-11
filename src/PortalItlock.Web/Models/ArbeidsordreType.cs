namespace PortalItlock.Web.Models;

public enum ArbeidsordreType
{
    Fastpris,
    TidMedgatt
}

public static class ArbeidsordreTypeExtensions
{
    public static string Visningsnavn(this ArbeidsordreType type) => type switch
    {
        ArbeidsordreType.Fastpris => "Fastpris",
        ArbeidsordreType.TidMedgatt => "Tid medgått",
        _ => type.ToString()
    };
}
