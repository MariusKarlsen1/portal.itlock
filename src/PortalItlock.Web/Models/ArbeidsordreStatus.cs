namespace PortalItlock.Web.Models;

// Planlagt ligger sist fordi statusen lagres som tall; visningsrekkefølgen står i Rekkefolge.
public enum ArbeidsordreStatus { Ny, Klar, Pagaende, Ferdig, Fakturert, Planlagt }

public static class ArbeidsordreStatusExtensions
{
    /// Rekkefølgen statusene vises i (nedtrekkslister, filtre).
    public static readonly ArbeidsordreStatus[] Rekkefolge =
        [ArbeidsordreStatus.Ny, ArbeidsordreStatus.Planlagt, ArbeidsordreStatus.Klar, ArbeidsordreStatus.Pagaende, ArbeidsordreStatus.Ferdig, ArbeidsordreStatus.Fakturert];

    public static string Visningsnavn(this ArbeidsordreStatus status) => status switch
    {
        ArbeidsordreStatus.Ny => "Ny",
        ArbeidsordreStatus.Planlagt => "Planlagt",
        ArbeidsordreStatus.Klar => "Klar",
        ArbeidsordreStatus.Pagaende => "Pågående",
        ArbeidsordreStatus.Ferdig => "Ferdig",
        ArbeidsordreStatus.Fakturert => "Fakturert",
        _ => status.ToString()
    };

    public static string PillIkon(this ArbeidsordreStatus status) => status switch
    {
        ArbeidsordreStatus.Ny => "bell",
        ArbeidsordreStatus.Planlagt => "calendar",
        ArbeidsordreStatus.Klar => "check-circle",
        ArbeidsordreStatus.Pagaende => "tool",
        ArbeidsordreStatus.Ferdig => "check-circle",
        ArbeidsordreStatus.Fakturert => "receipt",
        _ => "bell"
    };

    public static string PillFarge(this ArbeidsordreStatus status) => status switch
    {
        ArbeidsordreStatus.Ny => "blaa",
        ArbeidsordreStatus.Planlagt => "noytral",
        ArbeidsordreStatus.Klar => "lilla",
        ArbeidsordreStatus.Pagaende => "gul",
        ArbeidsordreStatus.Ferdig => "gronn",
        ArbeidsordreStatus.Fakturert => "noytral",
        _ => "noytral"
    };
}
