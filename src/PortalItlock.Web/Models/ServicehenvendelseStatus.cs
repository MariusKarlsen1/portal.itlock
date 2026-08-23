namespace PortalItlock.Web.Models;

public enum ServicehenvendelseStatus
{
    Ny,
    UnderVurdering,
    PrisGitt,
    Avslatt,
    Fullfort,
    VarerBestilt
}

public static class ServicehenvendelseStatusExtensions
{
    public static readonly ServicehenvendelseStatus[] Rekkefolge =
    [
        ServicehenvendelseStatus.Ny,
        ServicehenvendelseStatus.UnderVurdering,
        ServicehenvendelseStatus.PrisGitt,
        ServicehenvendelseStatus.VarerBestilt,
        ServicehenvendelseStatus.Fullfort,
        ServicehenvendelseStatus.Avslatt
    ];

    public static string Visningsnavn(this ServicehenvendelseStatus status) => status switch
    {
        ServicehenvendelseStatus.Ny => "Ny",
        ServicehenvendelseStatus.UnderVurdering => "Under vurdering",
        ServicehenvendelseStatus.PrisGitt => "Pris gitt",
        ServicehenvendelseStatus.VarerBestilt => "Varer bestilt",
        ServicehenvendelseStatus.Avslatt => "Avslått",
        ServicehenvendelseStatus.Fullfort => "Fullført",
        _ => status.ToString()
    };
}
