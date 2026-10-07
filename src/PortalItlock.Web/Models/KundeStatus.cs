namespace PortalItlock.Web.Models;

public enum KundeStatus
{
    Aktiv,
    Inaktiv
}

public static class KundeStatusExtensions
{
    public static string Visningsnavn(this KundeStatus status) => status switch
    {
        KundeStatus.Aktiv => "Aktiv",
        KundeStatus.Inaktiv => "Inaktiv",
        _ => status.ToString()
    };
}
