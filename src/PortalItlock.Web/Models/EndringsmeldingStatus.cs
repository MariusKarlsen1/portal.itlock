namespace PortalItlock.Web.Models;

public enum EndringsmeldingStatus { Utkast, SendtTilKunde, Godkjent, Avslatt }

public static class EndringsmeldingStatusExtensions
{
    public static string Visningsnavn(this EndringsmeldingStatus status) => status switch
    {
        EndringsmeldingStatus.Utkast => "Utkast",
        EndringsmeldingStatus.SendtTilKunde => "Sendt til kunde",
        EndringsmeldingStatus.Godkjent => "Godkjent",
        EndringsmeldingStatus.Avslatt => "Avslått",
        _ => status.ToString()
    };

    public static string PillIkon(this EndringsmeldingStatus status) => status switch
    {
        EndringsmeldingStatus.Utkast => "file-text",
        EndringsmeldingStatus.SendtTilKunde => "mail",
        EndringsmeldingStatus.Godkjent => "check-circle",
        EndringsmeldingStatus.Avslatt => "x",
        _ => "file-text"
    };

    public static string PillFarge(this EndringsmeldingStatus status) => status switch
    {
        EndringsmeldingStatus.Utkast => "noytral",
        EndringsmeldingStatus.SendtTilKunde => "gul",
        EndringsmeldingStatus.Godkjent => "gronn",
        EndringsmeldingStatus.Avslatt => "rod",
        _ => "noytral"
    };
}
