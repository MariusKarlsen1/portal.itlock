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
}
