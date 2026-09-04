namespace PortalItlock.Web.Models;

public enum CeGodkjenningStatus
{
    UnderArbeid,
    TilGjennomgang,
    Godkjent,
    Avvist
}

public static class CeGodkjenningStatusExtensions
{
    public static string Visningsnavn(this CeGodkjenningStatus status) => status switch
    {
        CeGodkjenningStatus.UnderArbeid => "Under arbeid",
        CeGodkjenningStatus.TilGjennomgang => "Til gjennomgang",
        CeGodkjenningStatus.Godkjent => "Godkjent",
        CeGodkjenningStatus.Avvist => "Avvist",
        _ => status.ToString()
    };
}
