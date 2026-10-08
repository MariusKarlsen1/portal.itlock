namespace PortalItlock.Web.Models;

public enum BefaringStatus
{
    Planlagt,
    Gjennomfort,
    Avlyst
}

public static class BefaringStatusExtensions
{
    public static string Visningsnavn(this BefaringStatus status) => status switch
    {
        BefaringStatus.Planlagt => "Planlagt",
        BefaringStatus.Gjennomfort => "Gjennomført",
        BefaringStatus.Avlyst => "Avlyst",
        _ => status.ToString()
    };
}
