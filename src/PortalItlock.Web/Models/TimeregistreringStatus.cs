namespace PortalItlock.Web.Models;

public enum TimeregistreringStatus
{
    Venter,
    Godkjent,
    Avslatt
}

public static class TimeregistreringStatusExtensions
{
    public static string Visningsnavn(this TimeregistreringStatus status) => status switch
    {
        TimeregistreringStatus.Venter => "Venter",
        TimeregistreringStatus.Godkjent => "Godkjent",
        TimeregistreringStatus.Avslatt => "Avslått",
        _ => status.ToString()
    };
}
