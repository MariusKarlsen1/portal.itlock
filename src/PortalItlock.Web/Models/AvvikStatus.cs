namespace PortalItlock.Web.Models;

public enum AvvikStatus
{
    Apent,
    SendtTilKunde,
    Godkjent,

    // Lagt til sist så lagrede tallverdier ikke endrer betydning.
    Lukket
}

public static class AvvikStatusExtensions
{
    public static string Visningsnavn(this AvvikStatus status) => status switch
    {
        AvvikStatus.Apent => "Åpent",
        AvvikStatus.SendtTilKunde => "Sendt til kunde",
        AvvikStatus.Godkjent => "Godkjent av kunde",
        AvvikStatus.Lukket => "Lukket",
        _ => status.ToString()
    };
}
