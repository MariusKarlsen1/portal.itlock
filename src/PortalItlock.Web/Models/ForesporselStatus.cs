namespace PortalItlock.Web.Models;

public enum ForesporselStatus
{
    Ny,
    UnderBehandling,
    VenterPaKunde,
    Ferdig
}

public enum ForesporselPrioritet
{
    Lav,
    Normal,
    Hoy
}

public static class ForesporselStatusExtensions
{
    public static readonly ForesporselStatus[] Rekkefolge =
    [
        ForesporselStatus.Ny,
        ForesporselStatus.UnderBehandling,
        ForesporselStatus.VenterPaKunde,
        ForesporselStatus.Ferdig
    ];

    public static string Visningsnavn(this ForesporselStatus status) => status switch
    {
        ForesporselStatus.Ny => "Ny",
        ForesporselStatus.UnderBehandling => "Under behandling",
        ForesporselStatus.VenterPaKunde => "Venter på kunde",
        ForesporselStatus.Ferdig => "Ferdig",
        _ => status.ToString()
    };

    public static string Farge(this ForesporselStatus status) => status switch
    {
        ForesporselStatus.Ny => "#2457c5",
        ForesporselStatus.UnderBehandling => "#c9902f",
        ForesporselStatus.VenterPaKunde => "#8a6ad0",
        ForesporselStatus.Ferdig => "#2e7d4f",
        _ => "#9b9189"
    };

    public static string Visningsnavn(this ForesporselPrioritet p) => p switch
    {
        ForesporselPrioritet.Lav => "Lav",
        ForesporselPrioritet.Normal => "Normal",
        ForesporselPrioritet.Hoy => "Høy",
        _ => p.ToString()
    };
}
