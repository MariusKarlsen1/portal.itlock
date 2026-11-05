using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Servicerapporten skal alltid kunne lages, uansett hvor lite eller mye som er
// fylt ut på runden. Mange kunder har tomme felt, og andre har lange tekster.
public class ServicerapportPdfTests
{
    private static readonly RapportProfil Org = RapportProfil.Fra(new Tenant { Navn = "Testfirma AS", ConnectionString = "" });

    private static bool ErPdf(byte[] pdf) =>
        pdf.Length > 1000 && System.Text.Encoding.ASCII.GetString(pdf, 0, 4) == "%PDF";

    private static int Sider(byte[] pdf) =>
        System.Text.RegularExpressions.Regex.Matches(System.Text.Encoding.Latin1.GetString(pdf), @"/Type\s*/Page[^s]").Count;

    private static Servicerunde Full() => new()
    {
        Id = 3,
        Dato = new DateTime(2026, 10, 15),
        StatusBeskrivelse = "Alle dører kontrollert.",
        Anbefalinger = "Bytt dørlukker innen neste runde.",
        NesteServiceDato = new DateTime(2027, 4, 26),
        Forside = "<p>Halvårlig service.</p>",
        Prosjekt = new Prosjekt { Navn = "Serviceavtale", Kunde = new Kunde { Navn = "Kunde AS", Adresse = "Havnegata 5" } },
        Deler = [new ServicerundeDel { Beskrivelse = "Lukkerarm", Feil = "Slo igjen" }, new ServicerundeDel { Beskrivelse = "Sylinder" }],
        Sjekkpunkter =
        [
            new ServicerundeSjekkpunkt { Tekst = "Smør låskasser", Rekkefolge = 1, Fullfort = true },
            new ServicerundeSjekkpunkt { Tekst = "Test nødutganger", Rekkefolge = 2 },
            new ServicerundeSjekkpunkt { Tekst = "Kontroller lukkere", Rekkefolge = 3, Fullfort = true }
        ]
    };

    [Fact]
    public void FullRunde_GirForsideOgEnRapportside()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var pdf = ServicerapportPdfService.Bygg(Full(), Org, null!);

        Assert.True(ErPdf(pdf));
        Assert.Equal(2, Sider(pdf));
    }

    [Fact]
    public void TomRunde_KanLagesUtenFeil()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var pdf = ServicerapportPdfService.Bygg(new Servicerunde { Id = 1, Dato = new DateTime(2026, 1, 2), ErPlanlagt = true, StatusBeskrivelse = "" }, Org, null!);

        Assert.True(ErPdf(pdf));
        Assert.Equal(2, Sider(pdf));
    }

    [Fact]
    public void BareNesteService_KanLagesUtenFeil()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var runde = new Servicerunde { Id = 2, Dato = new DateTime(2026, 1, 2), NesteServiceDato = new DateTime(2026, 7, 1), StatusBeskrivelse = "" };

        Assert.True(ErPdf(ServicerapportPdfService.Bygg(runde, Org, null!)));
    }

    [Fact]
    public void MyeInnhold_FortsetterPaaNyeSider()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var runde = Full();
        for (var i = 0; i < 60; i++)
        {
            runde.Deler.Add(new ServicerundeDel { Beskrivelse = $"Del {i}", Feil = "Slitt", Dato = new DateTime(2026, 1, 1).AddDays(i) });
            runde.Sjekkpunkter.Add(new ServicerundeSjekkpunkt { Tekst = $"Punkt {i}", Rekkefolge = i + 10, Fullfort = true });
        }

        var pdf = ServicerapportPdfService.Bygg(runde, Org, null!);

        Assert.True(ErPdf(pdf));
        Assert.True(Sider(pdf) > 2);
    }

    [Fact]
    public void LangForsidetekst_KanLagesUtenFeil()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var runde = Full();
        runde.Forside = string.Concat(Enumerable.Repeat("<p>Lang forsidetekst med betingelser som går over mange linjer i rapporten.</p>", 80));

        Assert.True(ErPdf(ServicerapportPdfService.Bygg(runde, Org, null!)));
    }

    private const string Punkter = """
        NyttBygg
        ID26.227 romnr 229 - Automatikken henger skeivt.
        ID25.105.4 romnr: 105a - Mangler en skilt skrue
        ID29.214.1 - Romnr: 214 - utvendig albuebryter fungerer ikke
        Gammelt Bygg
        14- mangler en skrue i dørvrider
        19 - Døren er ujevn og stanger i karm.
        22 - mangler en skrue i vrider.
        Nødkopper:
        Har tatt av nødkoppene for å sjekke om rømning er ivaretatt, og alle sitter som de skal.
        Kritisk:
        Nytt Bygg
        YD04.113.1 romnr: 113 - Får ikke opp døren. Rømningsdør.
        Gammelt Bygg:
        44- Dørpakningen ligger over dørlukkeren.
        """;

    [Fact]
    public void Tolk_DelerOppOverskrifterRaderOgKritisk()
    {
        var tolking = ServiceAnbefalinger.Tolk(Punkter);

        Assert.True(tolking.ErLang);
        Assert.Equal(6, tolking.Antall);
        Assert.Equal("Gammelt Bygg", tolking.Linjer[4].Tekst);
        Assert.Equal(ServiceAnbefalinger.LinjeType.Overskrift, tolking.Linjer[4].Type);
        Assert.Equal("Nødkopper", tolking.Linjer[8].Tekst);
        Assert.Equal(ServiceAnbefalinger.LinjeType.Avsnitt, tolking.Linjer[9].Type);
        Assert.Equal("14", tolking.Linjer[5].Dor);
        Assert.Equal("ID29.214.1", tolking.Linjer[3].Dor);
        Assert.Equal("Romnr: 214 - utvendig albuebryter fungerer ikke", tolking.Linjer[3].Tekst);
        Assert.Equal(["YD04.113.1 romnr: 113", "44"], tolking.Kritisk.Select(k => k.Dor));
    }

    [Fact]
    public void Tolk_LeserFormatertTekstLikt()
    {
        var html = string.Concat(Punkter.Split('\n').Select(l => $"<p>{l.Trim()}</p>"));

        Assert.Equal(ServiceAnbefalinger.Tolk(Punkter).Linjer, ServiceAnbefalinger.Tolk(html).Linjer);
        Assert.Equal(2, ServiceAnbefalinger.Tolk(html).Kritisk.Count);
    }

    [Fact]
    public void Tolk_KortTekstErIkkeLang()
    {
        Assert.False(ServiceAnbefalinger.Tolk("Bytt dørlukker innen neste runde.").ErLang);
        Assert.False(ServiceAnbefalinger.Tolk(null).ErLang);
    }

    [Fact]
    public void MedPunkter_ByttverStjernerILinjestart()
    {
        Assert.Equal("• Smurt\n• Sjekket 2 * 3", ServiceAnbefalinger.MedPunkter("* Smurt\n* Sjekket 2 * 3"));
        Assert.Equal("<p>• Smurt</p>", ServiceAnbefalinger.MedPunkter("<p>* Smurt</p>"));
    }

    [Fact]
    public void MangePunkter_FaarEgenSideEtterOppsummeringen()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var runde = Full();
        runde.Anbefalinger = Punkter;

        Assert.Equal(3, Sider(ServicerapportPdfService.Bygg(runde, Org, null!)));
    }

    [Fact]
    public void BareKritiskeLinjer_GirIngenEkstraSide()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var runde = Full();
        runde.Anbefalinger = "Kritisk:\n" + string.Join("\n", Enumerable.Range(1, 12).Select(i => $"{i} - Døren må sjekkes."));

        Assert.Equal(2, Sider(ServicerapportPdfService.Bygg(runde, Org, null!)));
    }

    [Fact]
    public void SvertLangeTekster_KanLagesUtenFeil()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var lang = string.Concat(Enumerable.Repeat("Lang tekst uten linjeskift som går over flere sider i rapporten. ", 800));
        var runde = Full();
        runde.StatusBeskrivelse = lang;
        runde.Anbefalinger = lang + "\n12 - " + lang + "\nKritisk:\n44 - " + lang;

        Assert.True(ErPdf(ServicerapportPdfService.Bygg(runde, Org, null!)));
    }
}
