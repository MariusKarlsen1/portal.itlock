namespace PortalItlock.Web.Services;

// Slår opp poststed fra postnummer via et lokalt bundlet snapshot av Postens
// offentlige postnummerregister (bring.no/postnummerregister-ansi.txt).
// Ingen live nettverkskall - Bring sin nyere API krever nå autentisering.
public class PostnummerService
{
    private readonly Lazy<Dictionary<string, string>> _register;

    public PostnummerService(IWebHostEnvironment env)
    {
        _register = new Lazy<Dictionary<string, string>>(() => LastRegister(env.ContentRootPath));
    }

    public string? SlaOpp(string? postnr)
    {
        if (string.IsNullOrWhiteSpace(postnr))
        {
            return null;
        }

        return _register.Value.TryGetValue(postnr.Trim(), out var sted) ? sted : null;
    }

    private static Dictionary<string, string> LastRegister(string contentRootPath)
    {
        var register = new Dictionary<string, string>();
        var filsti = Path.Combine(contentRootPath, "Data", "postnummerregister.txt");
        if (!File.Exists(filsti))
        {
            return register;
        }

        foreach (var linje in File.ReadLines(filsti))
        {
            var deler = linje.Split('\t');
            if (deler.Length >= 2 && !register.ContainsKey(deler[0]))
            {
                register[deler[0]] = deler[1];
            }
        }

        return register;
    }
}
