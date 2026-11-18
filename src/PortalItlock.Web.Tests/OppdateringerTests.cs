using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// «Hva er nytt»: hver versjon har et unikt nummer, nyeste øverst, og alle
// ikonene finnes i Icon.razor (ellers blir ikonfeltet tomt).
public class OppdateringerTests
{
    [Fact]
    public void Versjoner_ErGyldige()
    {
        Assert.NotEmpty(Oppdateringer.Alle);
        Assert.Equal(Oppdateringer.Alle.Count, Oppdateringer.Alle.Select(v => v.Nummer).Distinct().Count());
        var nummer = Oppdateringer.Alle.Select(v => Version.Parse(v.Nummer)).ToList();
        Assert.Equal(nummer.OrderByDescending(v => v), nummer);

        var ikonFil = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PortalItlock.Web", "Components", "Icon.razor");
        var ikoner = File.ReadAllText(ikonFil);
        foreach (var p in Oppdateringer.Alle.SelectMany(v => v.Punkter))
        {
            Assert.True(p.Tittel.Length <= 40, $"For lang tittel: {p.Tittel}");
            Assert.Contains($"case \"{p.Ikon}\":", ikoner);
        }
    }
}
