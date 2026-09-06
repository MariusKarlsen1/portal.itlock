namespace PortalItlock.Web.Services;

public static class GitEndringsloggLeser
{
    public record CommitInfo(string Sha, DateTime Dato, string Tittel, string Innhold);

    // Matcher feltskilletegnet %x1f (ASCII Unit Separator) i git log-formatet i Dockerfile.
    private const char FeltSkille = (char)0x1F;

    public static CommitInfo? Tolk(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var deler = raw.Split(FeltSkille);
        if (deler.Length < 4)
        {
            return null;
        }

        var sha = deler[0].Trim();
        if (string.IsNullOrWhiteSpace(sha) || !DateTimeOffset.TryParse(deler[1].Trim(), out var dato))
        {
            return null;
        }

        var tittel = deler[2].Trim();
        var innhold = RensBody(deler[3]);

        return new CommitInfo(sha, dato.DateTime, tittel, string.IsNullOrWhiteSpace(innhold) ? tittel : innhold);
    }

    private static string RensBody(string body)
    {
        var linjer = body
            .Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !l.StartsWith("Co-Authored-By:", StringComparison.OrdinalIgnoreCase)
                     && !l.StartsWith("Claude-Session:", StringComparison.OrdinalIgnoreCase));

        return string.Join("\n", linjer).Trim();
    }
}
