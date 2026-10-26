using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

/// <summary>
/// Årsprisen på en serviceavtale, regnet ut fra prismodellen som er valgt på
/// Prising-fanen: service-minutter (timepris × tid per runde × runder i året)
/// eller kategorisert årspris (pris pr dør × antall dører × krav per år).
/// </summary>
public static class ServiceavtalePris
{
    public const int Minutter = 0;
    public const int Kategorisert = 1;

    public sealed record Prislinje(string Tjeneste, string Pris);

    public static int MinutterPerRunde(IEnumerable<ServiceMinuttLinje> linjer) => linjer.Sum(l => (l.Minutter ?? 0) * l.Antall);

    public static decimal PrisPerRunde(int minutter, decimal? timepris) => (timepris ?? 0) * minutter / 60m;

    public static int RunderPerAr(Prosjekt p) => p.ServiceGangerPerAr is > 0 ? p.ServiceGangerPerAr.Value : 1;

    public static int AntallDorer(ServiceKategoriPris kp, IEnumerable<Dor> dorer) =>
        kp.ErEgendefinert ? kp.ManueltAntall ?? 0 : dorer.Count(d => DorServiceKategoriHelper.KategoriserDor(d) == kp.Kategori);

    public static decimal KategoriSum(ServiceKategoriPris kp, int antall) => (kp.Pris ?? 0) * antall * (kp.KravServicePrAar ?? 0);

    public static decimal Arspris(Prosjekt p, IReadOnlyCollection<Dor> dorer, IEnumerable<ServiceMinuttLinje> minutter, IEnumerable<ServiceKategoriPris> kategorier) =>
        p.ServicePrismodell == Kategorisert
            ? kategorier.Sum(kp => KategoriSum(kp, AntallDorer(kp, dorer)))
            : PrisPerRunde(MinutterPerRunde(minutter), p.ServiceTimepris) * RunderPerAr(p);

    public static List<Prislinje> Prislinjer(Prosjekt p)
    {
        if (!string.IsNullOrWhiteSpace(p.ServicePrislinjer))
        {
            try
            {
                return JsonSerializer.Deserialize<List<Prislinje>>(p.ServicePrislinjer) ?? [];
            }
            catch (JsonException)
            {
                // Faller tilbake til standardlinjene under.
            }
        }

        var timepris = p.ServiceTimepris is { } tp && tp > 0 ? $"kr {tp.ToString("#,##0", CultureInfo.GetCultureInfo("nb-NO"))} pr. time" : "";
        return
        [
            new("Servicemontør, ordinær arbeidstid", timepris),
            new("Servicemontør, overtid", "etter gjeldende satser"),
            new("Kjøring / oppmøte", ""),
            new("Påslag på deler og materiell", "")
        ];
    }

    /// Standard avtaletekst når avtalen ikke har sin egen. Kort og nøytral, med
    /// kunde, avsender og antall runder fylt inn - kan endres fritt på Prising-fanen.
    public static string StandardTekst(string org, string kunde, int runder)
    {
        static string E(string s) => WebUtility.HtmlEncode(s);
        var runderTekst = runder == 1 ? "1 servicerunde" : $"{runder} servicerunder";
        return $"""
<h2>Avtalens omfang</h2>
<p>Denne service- og vedlikeholdsavtalen gjelder dører og dørmiljø hos {E(kunde)}. Avtalen omfatter kontroll og justering av dørmiljøet etter fast sjekkliste, med servicerapport etter hver runde. Rapporten viser status, behov for ekstra service, reparasjoner og eventuelle deler som bør byttes.</p>
<p>Avtalen sikrer at dører, beslag, automatikker og adgangskontroll:</p>
<ul><li>fungerer riktig og sikkert i daglig bruk</li><li>opprettholder kravene til CE-samsvar, brann og rømning</li><li>vedlikeholdes etter gjeldende regler og produsentenes anvisninger</li></ul>
<p>Avtalen omfatter <b>{runderTekst} per år</b>. Tidspunkt avtales med kunden.</p>
<h2>Hva servicebesøket omfatter</h2>
<ul><li>gjennomgang og mekanisk justering av dører og dørmiljø</li><li>justering av dørlukkere og dørautomatikker</li><li>rengjøring og smøring av lås, beslag og bevegelige deler</li><li>kontroll av sensorer, sikkerhetsfunksjoner og styreenheter</li><li>funksjonstest av dører, lås og adgangskontroll</li></ul>
<h2>Dette omfattes ikke</h2>
<ul><li>deler og utstyr, og montering av disse</li><li>arbeid som følge av ytre skader, feil bruk, hærverk eller nye myndighetskrav</li></ul>
<p>Større reparasjoner og utskiftinger utføres etter avtale med kunden, til gjeldende satser.</p>
<h2>Varighet</h2>
<p>Avtalen gjelder for 12 måneder og fornyes automatisk for nye 12 måneder, med mindre den sies opp skriftlig senest 1 måned før utløp. Prisene reguleres årlig etter konsumprisindeksen (KPI).</p>
<h2>Rapportering og historikk</h2>
<p>Alle servicebesøk, avvik og byttede deler registreres fortløpende i Full Kontroll, slik at historikken for hver dør er tilgjengelig. {E(org)} varsler om feil og slitasje som faller utenfor avtalen, og anbefaler tiltak.</p>
""";
    }
}

/// <summary>
/// Tilbud på serviceavtale som PDF: forside, avtaletekst, priser og signaturer.
/// </summary>
public class ServiceavtaleTilbudPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public async Task<byte[]?> GenerateAsync(int prosjektId)
    {
        var p = await db.Prosjekter.AsNoTracking()
            .Include(x => x.Kunde)
            .Include(x => x.Prosjektleder)
            .FirstOrDefaultAsync(x => x.Id == prosjektId);
        if (p is null)
        {
            return null;
        }

        var dorer = await db.Dorer.AsNoTracking()
            .Where(d => d.ProsjektId == prosjektId)
            .Include(d => d.Komponenter).ThenInclude(dk => dk.Component)
            .Include(d => d.Funksjoner)
            .ToListAsync();
        var minutter = await db.ServiceMinuttLinjer.AsNoTracking().Where(l => l.ProsjektId == prosjektId).ToListAsync();
        var kategorier = await db.ServiceKategoriPriser.AsNoTracking().Where(k => k.ProsjektId == prosjektId).ToListAsync();

        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = p.Kunde?.Navn ?? "kunden";
        var runder = ServiceavtalePris.RunderPerAr(p);
        var arspris = ServiceavtalePris.Arspris(p, dorer, minutter, kategorier);
        var tekst = string.IsNullOrWhiteSpace(p.ServiceavtaleTekst) ? ServiceavtalePris.StandardTekst(org.Navn, kunde, runder) : p.ServiceavtaleTekst;
        var kontakt = p.Prosjektleder?.Navn ?? (org.ErStandard ? FirmaInfo.Kontaktperson : null);
        var dato = DateTime.Today;

        var prislinjer = new List<(string, string)>
        {
            ($"Serviceavtale – {(runder == 1 ? "1 runde" : $"{runder} runder")} per år", $"kr {arspris.ToString("#,##0", Kultur)} pr. år")
        };
        prislinjer.AddRange(ServiceavtalePris.Prislinjer(p).Where(l => !string.IsNullOrWhiteSpace(l.Tjeneste)).Select(l => (l.Tjeneste, l.Pris)));

        var dokument = Document.Create(container =>
        {
            container.Page(page => RapportMal.Forside(page, org, pdfLogo, "Tilbud", p.Navn, "Service- og vedlikeholdsavtale", "Tilbud", null,
                ("Kunde", p.Kunde?.Navn), ("Kontaktperson", kontakt), ("Dato", dato.ToString("dd.MM.yyyy"))));

            container.Page(page =>
            {
                RapportMal.Side(page);
                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Text("Service- og vedlikeholdsavtale").FontSize(20).Bold();
                    col.Item().Text($"Gjelder dører og dørmiljø omfattet av avtalen · {kunde}").FontSize(9.5f).Italic().FontColor(RapportMal.Dempet);
                    col.Item().Column(c => ForsideRenderer.Render(c, tekst));

                    col.Item().PaddingTop(6).Column(c =>
                    {
                        RapportMal.Overskrift(c, "Priser", org);
                        c.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(1); });
                            table.Header(h =>
                            {
                                h.Cell().BorderBottom(1.2f).BorderColor(RapportMal.Linje).PaddingVertical(4).Text("TJENESTE").FontSize(7).SemiBold().FontColor(RapportMal.Svak);
                                h.Cell().BorderBottom(1.2f).BorderColor(RapportMal.Linje).PaddingVertical(4).AlignRight().Text("PRIS EKS. MVA").FontSize(7).SemiBold().FontColor(RapportMal.Svak);
                            });
                            var forste = true;
                            foreach (var (tjeneste, pris) in prislinjer)
                            {
                                var v = table.Cell().BorderBottom(0.5f).BorderColor(RapportMal.Linje).PaddingVertical(5);
                                var h = table.Cell().BorderBottom(0.5f).BorderColor(RapportMal.Linje).PaddingVertical(5).AlignRight();
                                if (forste)
                                {
                                    v.Text(tjeneste).Bold();
                                    h.Text(pris).Bold();
                                    forste = false;
                                }
                                else
                                {
                                    v.Text(tjeneste);
                                    h.Text(string.IsNullOrWhiteSpace(pris) ? "–" : pris);
                                }
                            }
                        });
                        c.Item().PaddingTop(6).Text("Deler og materiell faktureres etter medgått forbruk dersom ikke annet er avtalt.").FontSize(8.5f).FontColor(RapportMal.Dempet);
                    });

                    col.Item().PaddingTop(16).ShowEntire().Column(c =>
                    {
                        RapportMal.Overskrift(c, "Signaturer", org);
                        foreach (var (part, datoTekst) in new[] { (kunde, ""), (org.Navn, dato.ToString("dd.MM.yyyy")) })
                        {
                            c.Item().PaddingTop(14).Text($"For {part}:").FontSize(9).FontColor(RapportMal.Dempet);
                            c.Item().PaddingTop(22).LineHorizontal(0.75f).LineColor(RapportMal.Svak);
                            c.Item().PaddingTop(4).Text($"Dato: {(string.IsNullOrEmpty(datoTekst) ? "____________________" : datoTekst)}").FontSize(9).FontColor(RapportMal.Dempet);
                        }
                    });
                });
                page.Footer().Element(e => RapportMal.Bunn(e, org));
            });
        });

        return dokument.GeneratePdf();
    }
}
