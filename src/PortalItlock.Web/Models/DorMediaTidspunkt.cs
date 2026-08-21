namespace PortalItlock.Web.Models;

public enum DorMediaTidspunkt
{
    For,
    Etter
}

public static class DorMediaTidspunktExtensions
{
    public static string Visningsnavn(this DorMediaTidspunkt tidspunkt) => tidspunkt switch
    {
        DorMediaTidspunkt.For => "Før",
        DorMediaTidspunkt.Etter => "Etter",
        _ => tidspunkt.ToString()
    };
}
