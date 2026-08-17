namespace PortalItlock.Web.Models;

public enum ArbeidsordreStatus { Ny, Klar, Pagaende, Ferdig }

public static class ArbeidsordreStatusExtensions
{
    public static string Visningsnavn(this ArbeidsordreStatus status) => status switch
    {
        ArbeidsordreStatus.Ny => "Ny",
        ArbeidsordreStatus.Klar => "Klar",
        ArbeidsordreStatus.Pagaende => "Pågående",
        ArbeidsordreStatus.Ferdig => "Ferdig",
        _ => status.ToString()
    };
}
