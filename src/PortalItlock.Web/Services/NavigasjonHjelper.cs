namespace PortalItlock.Web.Services;

public static class NavigasjonHjelper
{
    // Regner ut "ett steg tilbake mot forsiden" ut fra selve stien, i stedet
    // for å stole på faktisk navigasjonshistorikk (som hopper rundt avhengig
    // av hvor man kom fra - f.eks. en arbeidsordre åpnet fra kalenderen i
    // stedet for fra arbeidsordre-listen ville tidligere gått "tilbake" til
    // kalenderen). Strippes ett nivå (id/"ny"/"pdf") av gangen til man når en
    // topp-nivå-side, som alltid går rett til forsiden - en fullstendig
    // modul-hierarki-tabell er for skjør til å holde 100 % oppdatert etter
    // hvert som moduler flyttes rundt, så ukjente/dypere mønstre går også
    // rett til forsiden i stedet for å gjette seg frem til en sti som kanskje
    // ikke finnes.
    public static string FinnForelderSti(string naverendeSti)
    {
        var sti = naverendeSti.Split('?', '#')[0].Trim('/');
        if (sti.Length == 0)
        {
            return "/";
        }

        var segmenter = sti.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmenter.Length == 2 && (int.TryParse(segmenter[1], out _) || segmenter[1] is "ny" or "pdf")
            && FinnesSide("/" + segmenter[0]))
        {
            return "/" + segmenter[0];
        }

        return "/";
    }

    // Sider som finnes uten parametre (f.eks. /prosjekter). «/plantegning» finnes
    // ikke - da ga «Tilbake» før en 404-side.
    private static readonly Lazy<HashSet<string>> Sider = new(() =>
        typeof(NavigasjonHjelper).Assembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), false)
                .Cast<Microsoft.AspNetCore.Components.RouteAttribute>())
            .Select(r => "/" + r.Template.Trim('/'))
            .Where(t => !t.Contains('{'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase));

    public static bool FinnesSide(string sti) => Sider.Value.Contains("/" + sti.Trim('/'));
}
