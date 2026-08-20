namespace PortalItlock.Web.Models;

public enum ServicehenvendelseStatus
{
    Ny,
    UnderVurdering,
    PrisGitt,
    Avslatt,
    Fullfort
}

public static class ServicehenvendelseStatusExtensions
{
    public static string Visningsnavn(this ServicehenvendelseStatus status) => status switch
    {
        ServicehenvendelseStatus.Ny => "Ny",
        ServicehenvendelseStatus.UnderVurdering => "Under vurdering",
        ServicehenvendelseStatus.PrisGitt => "Pris gitt",
        ServicehenvendelseStatus.Avslatt => "Avslått",
        ServicehenvendelseStatus.Fullfort => "Fullført",
        _ => status.ToString()
    };
}
