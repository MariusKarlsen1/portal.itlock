namespace PortalItlock.Web.Models;

// Hva timene faktisk gikk med til. Skiller seg fra TimeregistreringType, som
// sier hvordan timene skal avlønnes (normal/overtid/avspasering).
public enum TimeAktivitet
{
    Montering,
    Programmering,
    Service,
    Befaring,
    Mote,
    Dokumentasjon,
    Kjoring,
    Intern
}

public static class TimeAktivitetExtensions
{
    public static string Visningsnavn(this TimeAktivitet aktivitet) => aktivitet switch
    {
        TimeAktivitet.Montering => "Montering",
        TimeAktivitet.Programmering => "Programmering",
        TimeAktivitet.Service => "Service",
        TimeAktivitet.Befaring => "Befaring",
        TimeAktivitet.Mote => "Møte",
        TimeAktivitet.Dokumentasjon => "Dokumentasjon",
        TimeAktivitet.Kjoring => "Kjøring",
        TimeAktivitet.Intern => "Intern",
        _ => aktivitet.ToString()
    };
}
