namespace PortalItlock.Web.Data;

// Plasserer platform.db og nye kunde-databaser i samme mappe som den
// opprinnelige SQLite-filen peker til, slik at alt havner på samme skrivbare
// Railway-volum uten at noe nytt må settes opp der.
public static class PlatformConnectionStringHelper
{
    public static string AvledFra(string applikasjonsConnectionString) =>
        $"Data Source={Path.Combine(FinnDataMappe(applikasjonsConnectionString), "platform.db")}";

    public static string FinnDataMappe(string connectionString)
    {
        var dataSource = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(del => del.Split('=', 2))
            .Where(par => par.Length == 2 && par[0].Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            .Select(par => par[1])
            .FirstOrDefault()
            ?? "portalitlock.db";

        var mappe = Path.GetDirectoryName(dataSource);
        return string.IsNullOrEmpty(mappe) ? "." : mappe;
    }
}
