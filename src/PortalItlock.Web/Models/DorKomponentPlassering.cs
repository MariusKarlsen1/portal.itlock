namespace PortalItlock.Web.Models;

public enum DorKomponentPlassering
{
    Hengselside,
    Karmside
}

public static class DorKomponentPlasseringExtensions
{
    public static string Kode(this DorKomponentPlassering plassering) => plassering switch
    {
        DorKomponentPlassering.Hengselside => "H",
        DorKomponentPlassering.Karmside => "K",
        _ => plassering.ToString()
    };

    public static string Visningsnavn(this DorKomponentPlassering plassering) => plassering switch
    {
        DorKomponentPlassering.Hengselside => "H - Hengselside",
        DorKomponentPlassering.Karmside => "K - Karmside",
        _ => plassering.ToString()
    };
}
