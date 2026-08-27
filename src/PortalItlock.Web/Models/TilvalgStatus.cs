namespace PortalItlock.Web.Models;

public enum TilvalgStatus
{
    Utkast,
    Publisert,
    Besvart
}

public static class TilvalgStatusExtensions
{
    public static string Visningsnavn(this TilvalgStatus status) => status switch
    {
        TilvalgStatus.Utkast => "Utkast",
        TilvalgStatus.Publisert => "Venter på svar fra kunde",
        TilvalgStatus.Besvart => "Besvart",
        _ => status.ToString()
    };
}
