namespace PortalItlock.Web.Models;

public enum ArbeidsordreStatus { Ny, Klar, Pagaende, Ferdig, Fakturert }

public static class ArbeidsordreStatusExtensions
{
    public static string Visningsnavn(this ArbeidsordreStatus status) => status switch
    {
        ArbeidsordreStatus.Ny => "Ny",
        ArbeidsordreStatus.Klar => "Klar",
        ArbeidsordreStatus.Pagaende => "Pågående",
        ArbeidsordreStatus.Ferdig => "Ferdig",
        ArbeidsordreStatus.Fakturert => "Fakturert",
        _ => status.ToString()
    };
}
