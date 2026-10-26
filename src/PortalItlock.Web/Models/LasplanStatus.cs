namespace PortalItlock.Web.Models;

public enum LasplanStatus
{
    IArbeid = 0,
    SendtTilKunde = 1,
    Godkjent = 2
}

public static class LasplanStatusExtensions
{
    public static string Visningsnavn(this LasplanStatus status) => status switch
    {
        LasplanStatus.SendtTilKunde => "Sendt til kunde",
        LasplanStatus.Godkjent => "Godkjent",
        _ => "I arbeid"
    };
}
