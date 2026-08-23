namespace PortalItlock.Web.Models;

public enum FravarStatus
{
    Venter,
    Godkjent,
    Avslatt
}

public static class FravarStatusExtensions
{
    public static string Visningsnavn(this FravarStatus status) => status switch
    {
        FravarStatus.Venter => "Venter",
        FravarStatus.Godkjent => "Godkjent",
        FravarStatus.Avslatt => "Avslått",
        _ => status.ToString()
    };
}
