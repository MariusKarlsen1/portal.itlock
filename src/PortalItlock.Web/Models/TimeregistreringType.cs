namespace PortalItlock.Web.Models;

public enum TimeregistreringType { NormalArbeidstid, Overtid50, Overtid100, Avspasering }

public static class TimeregistreringTypeExtensions
{
    public static string Visningsnavn(this TimeregistreringType type) => type switch
    {
        TimeregistreringType.NormalArbeidstid => "Normal arbeidstid",
        TimeregistreringType.Overtid50 => "50% overtid",
        TimeregistreringType.Overtid100 => "100% overtid",
        TimeregistreringType.Avspasering => "Avspasering",
        _ => type.ToString()
    };
}
