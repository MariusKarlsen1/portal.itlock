namespace PortalItlock.Web.Models;

public enum BrukerRolle
{
    Admin,
    Prosjektleder,
    Montor,
    Kunde
}

public static class BrukerRolleExtensions
{
    public static string Visningsnavn(this BrukerRolle rolle) => rolle switch
    {
        BrukerRolle.Admin => "Admin",
        BrukerRolle.Prosjektleder => "Prosjektleder",
        BrukerRolle.Montor => "Montør",
        BrukerRolle.Kunde => "Kunde",
        _ => rolle.ToString()
    };
}
