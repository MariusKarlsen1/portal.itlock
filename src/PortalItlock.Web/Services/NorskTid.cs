namespace PortalItlock.Web.Services;

/// <summary>
/// Klokka i Norge, uavhengig av serverens tidssone. Serveren kjører i UTC,
/// så DateTime.Now ga en «nå»-strek i kalenderen som lå 1-2 timer bak
/// (avhengig av sommer- eller vintertid).
/// </summary>
public static class NorskTid
{
    private static readonly TimeZoneInfo? Sone = FinnSone();

    public static DateTime Na => Sone is null
        ? DateTime.Now
        : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Sone);

    public static DateTime Idag => Na.Date;

    private static TimeZoneInfo? FinnSone()
    {
        foreach (var id in new[] { "Europe/Oslo", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return null;
    }
}
