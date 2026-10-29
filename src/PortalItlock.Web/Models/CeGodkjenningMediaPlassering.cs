namespace PortalItlock.Web.Models;

public enum CeGodkjenningMediaPlassering
{
    Hengselside,
    Karmside,
    Video,
    Typeskilt
}

public static class CeGodkjenningMediaPlasseringExtensions
{
    public static string Visningsnavn(this CeGodkjenningMediaPlassering plassering) => plassering switch
    {
        CeGodkjenningMediaPlassering.Hengselside => "Hengselside",
        CeGodkjenningMediaPlassering.Karmside => "Karmside",
        CeGodkjenningMediaPlassering.Video => "Video",
        CeGodkjenningMediaPlassering.Typeskilt => "Typeskilt",
        _ => plassering.ToString()
    };
}
