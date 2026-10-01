namespace PortalItlock.Web.Data;

// Plasserer platform.db (katalogen over kunder) i samme mappe som den
// vanlige SQLite-filen peker til, slik at den havner på samme skrivbare
// Railway-volum uten at noe nytt må settes opp der.
public static class PlatformConnectionStringHelper
{
    public static string AvledFra(string applikasjonsConnectionString)
    {
        var dataSource = applikasjonsConnectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(del => del.Split('=', 2))
            .Where(par => par.Length == 2 && par[0].Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            .Select(par => par[1])
            .FirstOrDefault()
            ?? "portalitlock.db";

        var mappe = Path.GetDirectoryName(dataSource);
        var platformFil = string.IsNullOrEmpty(mappe) ? "platform.db" : Path.Combine(mappe, "platform.db");

        return $"Data Source={platformFil}";
    }
}
