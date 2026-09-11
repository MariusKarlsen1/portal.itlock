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

    public static string PillIkon(this ArbeidsordreStatus status) => status switch
    {
        ArbeidsordreStatus.Ny => "bell",
        ArbeidsordreStatus.Klar => "check-circle",
        ArbeidsordreStatus.Pagaende => "tool",
        ArbeidsordreStatus.Ferdig => "check-circle",
        ArbeidsordreStatus.Fakturert => "receipt",
        _ => "bell"
    };

    public static string PillFarge(this ArbeidsordreStatus status) => status switch
    {
        ArbeidsordreStatus.Ny => "blaa",
        ArbeidsordreStatus.Klar => "lilla",
        ArbeidsordreStatus.Pagaende => "gul",
        ArbeidsordreStatus.Ferdig => "gronn",
        ArbeidsordreStatus.Fakturert => "noytral",
        _ => "noytral"
    };
}
