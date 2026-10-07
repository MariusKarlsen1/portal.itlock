namespace PortalItlock.Web.Models;

public enum TenantType
{
    Privat,
    Entreprise,
    Offentlig
}

public static class TenantTypeExtensions
{
    public static string Visningsnavn(this TenantType type) => type switch
    {
        TenantType.Privat => "Privat",
        TenantType.Entreprise => "Entreprise",
        TenantType.Offentlig => "Offentlig",
        _ => type.ToString()
    };
}
