namespace PortalItlock.Web.Models;

public enum NokkelsystemStatus
{
    Aktiv,
    Inaktiv
}

public static class NokkelsystemStatusExtensions
{
    public static string Visningsnavn(this NokkelsystemStatus status) => status switch
    {
        NokkelsystemStatus.Aktiv => "Aktiv",
        NokkelsystemStatus.Inaktiv => "Inaktiv",
        _ => status.ToString()
    };
}
