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
}
