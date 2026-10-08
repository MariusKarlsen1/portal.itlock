namespace PortalItlock.Web.Models;

public enum BefaringStatus
{
    Utkast,
    Planlagt,
    UnderBefaring,
    Gjennomfort,
    Avlyst
}

public static class BefaringStatusExtensions
{
    public static string Visningsnavn(this BefaringStatus status) => status switch
    {
        BefaringStatus.Utkast => "Utkast",
        BefaringStatus.Planlagt => "Planlagt",
        BefaringStatus.UnderBefaring => "Under befaring",
        BefaringStatus.Gjennomfort => "Gjennomført",
        BefaringStatus.Avlyst => "Avlyst",
        _ => status.ToString()
    };
}
