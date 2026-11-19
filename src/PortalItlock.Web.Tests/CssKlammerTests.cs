namespace PortalItlock.Web.Tests;

// Én manglende «}» i en .razor.css ødelegger alle stilene som kommer etter i den
// samlede stilfilen (PortalItlock.Web.styles.css) – hele portalen mister stilene.
// Sjekker at alle stilfiler har like mange { som }.
public class CssKlammerTests
{
    private static string WebMappe()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "PortalItlock.Web")))
        {
            dir = dir.Parent;
        }
        return Path.Combine(dir!.FullName, "PortalItlock.Web");
    }

    [Fact]
    public void AlleStilfiler_HarLukkedeKlammer()
    {
        var web = WebMappe();
        var filer = Directory.GetFiles(Path.Combine(web, "Components"), "*.css", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(web, "wwwroot"), "*.css", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bootstrap") && !f.EndsWith(".min.css")));
        var feil = new List<string>();
        foreach (var f in filer)
        {
            var dybde = 0;
            var iKommentar = false;
            var tekst = File.ReadAllText(f);
            for (var i = 0; i < tekst.Length; i++)
            {
                if (!iKommentar && tekst[i] == '/' && i + 1 < tekst.Length && tekst[i + 1] == '*') { iKommentar = true; i++; continue; }
                if (iKommentar && tekst[i] == '*' && i + 1 < tekst.Length && tekst[i + 1] == '/') { iKommentar = false; i++; continue; }
                if (iKommentar) continue;
                if (tekst[i] == '{') dybde++;
                else if (tekst[i] == '}') dybde--;
            }
            if (dybde != 0)
            {
                feil.Add($"{Path.GetRelativePath(web, f)} ({dybde:+#;-#})");
            }
        }
        Assert.True(feil.Count == 0, "Ulikt antall { og } i: " + string.Join(", ", feil));
    }
}
