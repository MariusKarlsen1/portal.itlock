using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Prismodell «Antall» for serviceavtaler (Prototype serviceavtale-ny-prising-tilbud.html):
/// kategorier fra Oppsett med antall per runde × pris, pluss tillegg (oppmøte,
/// kjøring, påslag …), ganger runder i året, minus rabatt. Avtalen starter som et
/// tilbud med fanene Prising, Tillegg og Tilbud, og blir en vanlig serviceavtale når
/// tilbudet godkjennes.
/// </summary>
public static class ServiceavtaleAntall
{
    public static readonly (string Navn, string Beskrivelse, decimal Pris)[] StandardKategorier =
    [
        ("Dørautomatikk", "Slagdørsautomatikk, åpner og lukker", 1250),
        ("Skyvedørsautomatikk", "Skyvedør med sensorer og nødåpning", 1650),
        ("Mekanisk", "Låskasse, sylinder, beslag og dørlukker", 350),
        ("2-fløyet dør", "To dørblad, koordinator og stangbeslag", 600),
        ("Adgangskontroll", "Kortleser, elektrisk lås og sentral", 900),
    ];

    public static readonly (string Navn, string Type, decimal? Pris)[] StandardTillegg =
    [
        ("Fast oppmøte", ServiceavtaleLinje.PerRunde, null),
        ("Kjøring", ServiceavtaleLinje.PerKm, null),
        ("Fast påslag", ServiceavtaleLinje.Prosent, null),
    ];

    public static readonly (string Kode, string Navn)[] Typer =
    [
        (ServiceavtaleLinje.PerRunde, "kr per runde"),
        (ServiceavtaleLinje.PerKm, "kr per km"),
        (ServiceavtaleLinje.PerAr, "kr per år"),
        (ServiceavtaleLinje.Prosent, "% påslag"),
    ];

    public static string TypeNavn(string? type) => Typer.FirstOrDefault(t => t.Kode == type).Navn ?? "kr per runde";

    // Valg for hva tilbuds-PDF-en viser.
    public const string VisLinjer = "linjer";
    public const string VisStk = "stk";
    public const string VisRunde = "runde";
    public const string VisMnd = "mnd";
    public const string VisSignatur = "signatur";
    public const string StandardValg = "linjer,runde,signatur";

    public static HashSet<string> Valg(Prosjekt p) =>
        (p.ServiceTilbudValg ?? StandardValg).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

    public sealed record Beregning(decimal PerRunde, decimal Kategorier, decimal Tillegg, decimal Brutto, decimal Rabatt, decimal Arspris, int Runder)
    {
        public decimal Mva => Math.Round(Arspris * 0.25m, 2);
        public decimal InklMva => Arspris + Mva;
        public decimal PerManed => Math.Round(Arspris / 12, 2);
    }

    /// Kategoriene per runde (antall × pris).
    public static decimal PerRunde(IEnumerable<ServiceavtaleLinje> linjer) =>
        linjer.Where(l => !l.ErTillegg).Sum(l => Math.Max(0, l.Antall) * Math.Max(0, l.Pris));

    /// Ett tillegg per år. Prosent regnes av kategoriene per år.
    public static decimal TilleggPerAr(ServiceavtaleLinje t, decimal kategorierPerAr, int runder) =>
        t.Pris <= 0 ? 0 : t.TilleggType switch
        {
            ServiceavtaleLinje.Prosent => Math.Round(kategorierPerAr * t.Pris / 100, 2),
            ServiceavtaleLinje.PerAr => t.Pris * Math.Max(0, t.Antall),
            _ => t.Pris * Math.Max(0, t.Antall) * runder
        };

    public static Beregning Beregn(Prosjekt p, IEnumerable<ServiceavtaleLinje> linjer)
    {
        var liste = linjer.ToList();
        var runder = ServiceavtalePris.RunderPerAr(p);
        var perRunde = PerRunde(liste);
        var kategorier = perRunde * runder;
        var tillegg = liste.Where(l => l.ErTillegg).Sum(t => TilleggPerAr(t, kategorier, runder));
        var brutto = kategorier + tillegg;
        var rabatt = p.ServiceRabattProsent is > 0 ? Math.Round(brutto * Math.Min(100, p.ServiceRabattProsent.Value) / 100, 2) : 0;
        return new Beregning(perRunde, kategorier, tillegg, brutto, rabatt, brutto - rabatt, runder);
    }

    /// Fyller inn standardkategorier og -tillegg i Oppsett første gang.
    public static async Task SikreStandard(ApplicationDbContext db)
    {
        if (!await db.ServicePrisKategorier.AnyAsync())
        {
            var i = 0;
            foreach (var (navn, besk, pris) in StandardKategorier)
            {
                db.ServicePrisKategorier.Add(new ServicePrisKategori { Navn = navn, Beskrivelse = besk, Pris = pris, Rekkefolge = i++ });
            }
            var j = 0;
            if (!await db.ServiceTilleggMaler.AnyAsync())
            {
                foreach (var (navn, type, pris) in StandardTillegg)
                {
                    db.ServiceTilleggMaler.Add(new ServiceTilleggMal { Navn = navn, Type = type, Pris = pris, Rekkefolge = j++ });
                }
            }
            await db.SaveChangesAsync();
        }
    }

    /// Linjene en ny avtale får: kategoriene slik de står i Oppsett nå. Tillegg legges til
    /// fra + på Prising når de trengs.
    public static async Task<List<ServiceavtaleLinje>> LagLinjer(ApplicationDbContext db, int prosjektId)
    {
        await SikreStandard(db);
        var kat = await db.ServicePrisKategorier.AsNoTracking().OrderBy(k => k.Rekkefolge).ThenBy(k => k.Id).ToListAsync();
        var linjer = new List<ServiceavtaleLinje>();
        var r = 0;
        foreach (var k in kat)
        {
            linjer.Add(new ServiceavtaleLinje { ProsjektId = prosjektId, Navn = k.Navn, Beskrivelse = k.Beskrivelse, Pris = k.Pris, StandardPris = k.Pris, Rekkefolge = r++ });
        }
        return linjer;
    }

    /// Fletter inn {kunde}, {bygg}, {adresse}, {runder} og {pris} i tilbudsteksten (HTML).
    public static string Flett(string html, Prosjekt p, Beregning b)
    {
        static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
        return html
            .Replace("{kunde}", E(p.Kunde?.Navn ?? "kunden"), StringComparison.OrdinalIgnoreCase)
            .Replace("{bygg}", E(p.Navn), StringComparison.OrdinalIgnoreCase)
            .Replace("{adresse}", E(p.Adresse), StringComparison.OrdinalIgnoreCase)
            .Replace("{runder}", b.Runder.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{pris}", Kr(b.Arspris), StringComparison.OrdinalIgnoreCase);
    }

    public static string Kr(decimal v) => Math.Round(v).ToString("#,##0", CultureInfo.GetCultureInfo("nb-NO")) + " kr";

    /// Standardtekst på tilbudet når avtalen ikke har sin egen. Kunde, bygg, runder og
    /// datoer står fast på forsiden, og prisen i tabellen – teksten trenger dem ikke.
    public static string StandardTekst(string org) =>
        "<h2>Om avtalen</h2>" +
        "<p>Takk for forespørselen. Vi har gleden av å gi tilbud på serviceavtale for dører, beslag, automatikk og adgangskontroll.</p>" +
        "<p>På hver servicerunde kontrollerer, justerer og smører vi utstyret, og retter feil vi finner. Avvik og utført arbeid dokumenteres i en servicerapport etter hver runde.</p>" +
        "<h3>Forbehold</h3>" +
        "<ul><li>Avtalen løper i 12 måneder og fornyes automatisk med 3 måneders oppsigelse.</li>" +
        "<li>Prisene justeres årlig etter konsumprisindeksen.</li>" +
        "<li>Deler og reparasjoner utover service faktureres etter medgått tid og materiell.</li></ul>" +
        $"<p>Med vennlig hilsen<br>{System.Net.WebUtility.HtmlEncode(org)}</p>";
}
