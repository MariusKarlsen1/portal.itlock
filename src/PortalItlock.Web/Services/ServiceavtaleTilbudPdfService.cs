using System.Globalization;
using System.Net;
using System.Text.Json;
using AngleSharp.Html.Parser;
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
    // Antall per kategori × pris fra Oppsett, pluss tillegg (ServiceavtaleAntall).
    public const int Antall = 2;

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
/// Serviceavtale som PDF, malen «Sandbånd» (oktober 2026): et rolig bånd øverst i
/// organisasjonens lyse farge med kunde og nøkkeltall, og avtaleteksten under med
/// hver seksjon som en rad (overskrift til venstre, tekst til høyre). Ingen forside,
/// priser eller signaturer - avtalen skal se ut som en avtale, ikke en rapport.
/// Logo, navn, farge og kontaktinfo hentes fra organisasjonen som er logget inn.
/// </summary>
public class ServiceavtaleTilbudPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public sealed record Seksjon(string? Tittel, string Html);

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

        var antallDorer = await db.Dorer.CountAsync(d => d.ProsjektId == prosjektId);
        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = p.Kunde?.Navn ?? "kunden";
        var runder = ServiceavtalePris.RunderPerAr(p);
        // Prismodell «Antall»: linjene fra Prising og Tillegg, og {kunde}/{pris} … flettes inn i teksten.
        var antallModell = p.ServicePrismodell == ServiceavtalePris.Antall;
        var linjer = antallModell ? await db.ServiceavtaleLinjer.AsNoTracking().Where(l => l.ProsjektId == prosjektId).OrderBy(l => l.Rekkefolge).ThenBy(l => l.Id).ToListAsync() : [];
        var beregning = antallModell ? ServiceavtaleAntall.Beregn(p, linjer) : null;
        var valg = ServiceavtaleAntall.Valg(p);
        var tekst = antallModell
            ? ServiceavtaleAntall.Flett(string.IsNullOrWhiteSpace(p.ServiceavtaleTekst) ? ServiceavtaleAntall.StandardTekst(org.Navn) : p.ServiceavtaleTekst, p, beregning!)
            : string.IsNullOrWhiteSpace(p.ServiceavtaleTekst) ? ServiceavtalePris.StandardTekst(org.Navn, kunde, runder) : p.ServiceavtaleTekst;
        var kontakt = p.Prosjektleder?.Navn ?? (org.ErStandard ? FirmaInfo.Kontaktperson : null);
        var seksjoner = Seksjoner(tekst);

        var nokkeltall = new (string Label, string? Verdi)[]
        {
            ("Dato", NorskTid.Idag.ToString("dd.MM.yyyy")),
            ("Besøk", runder == 1 ? "1 per år" : $"{runder} per år"),
            ("Dører", antallDorer > 0 ? antallDorer.ToString(CultureInfo.InvariantCulture) : null),
            ("Kontaktperson", kontakt)
        };

        var dokument = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(PdfStil.Ink).LineHeight(1.4f));

                // Båndet vises bare på første side; på neste sider holdes vanlig toppmarg.
                page.Header().Column(h =>
                {
                    h.Item().ShowOnce().Background(org.AksentLys).BorderBottom(3).BorderColor(org.Aksent)
                        .PaddingHorizontal(2, Unit.Centimetre).PaddingTop(1.8f, Unit.Centimetre).PaddingBottom(1.3f, Unit.Centimetre)
                        .Column(b =>
                        {
                            b.Item().Element(e => org.TegnLogo(e, pdfLogo, 26));
                            b.Item().PaddingTop(28).Element(e => Etikett(e, "Serviceavtale", org.Aksent));
                            b.Item().PaddingTop(3).Text(kunde).FontSize(26).SemiBold().LineHeight(1.1f);
                            if (!string.Equals(p.Navn, kunde, StringComparison.OrdinalIgnoreCase))
                            {
                                b.Item().PaddingTop(4).Text(p.Navn).FontSize(11).FontColor(RapportMal.Dempet);
                            }
                            b.Item().PaddingTop(24).Row(row =>
                            {
                                foreach (var (label, verdi) in nokkeltall.Where(n => !string.IsNullOrWhiteSpace(n.Verdi)))
                                {
                                    row.AutoItem().PaddingRight(30).Column(c =>
                                    {
                                        c.Item().Element(e => Etikett(e, label, RapportMal.Svak));
                                        c.Item().PaddingTop(1).Text(verdi).FontSize(9.5f).SemiBold();
                                    });
                                }
                            });
                        });
                    h.Item().SkipOnce().Height(1.6f, Unit.Centimetre);
                });

                page.Content().PaddingHorizontal(2, Unit.Centimetre).PaddingTop(1.1f, Unit.Centimetre).Column(col =>
                {
                    var forste = true;
                    foreach (var s in seksjoner)
                    {
                        var rad = col.Item();
                        if (!forste)
                        {
                            rad = rad.PaddingTop(10).BorderTop(0.75f).BorderColor(RapportMal.Linje).PaddingTop(10);
                        }
                        forste = false;
                        rad.Row(row =>
                        {
                            row.ConstantItem(100).PaddingTop(2).PaddingRight(12)
                                .Element(e => Etikett(e, s.Tittel ?? "Avtale", org.Aksent));
                            row.RelativeItem().Column(c => ForsideRenderer.Render(c, s.Html));
                        });
                    }

                    if (beregning is not null)
                    {
                        col.Item().PaddingTop(16).Element(e => Etikett(e, "Pris", org.Aksent));
                        col.Item().PaddingTop(6).Element(e => Pristabell(e, linjer, beregning, valg));
                        if (valg.Contains(ServiceavtaleAntall.VisSignatur))
                        {
                            col.Item().PaddingTop(46).Row(r =>
                            {
                                r.RelativeItem().BorderTop(0.75f).BorderColor(RapportMal.Linje).PaddingTop(4)
                                    .Text($"Sted / dato / signatur – {kunde}").FontSize(8).FontColor(RapportMal.Dempet);
                                r.ConstantItem(40);
                                r.RelativeItem().BorderTop(0.75f).BorderColor(RapportMal.Linje).PaddingTop(4)
                                    .Text($"Sted / dato / signatur – {org.Navn}").FontSize(8).FontColor(RapportMal.Dempet);
                            });
                        }
                    }
                });

                page.Footer().PaddingHorizontal(2, Unit.Centimetre).PaddingBottom(1.2f, Unit.Centimetre).Element(e => RapportMal.Bunn(e, org));
            });
        });

        return dokument.GeneratePdf();
    }

    // Prismodell «Antall»: kategoriene med antall, tilleggene og summen. Tynne linjer, ingen grå rader (PDF-stil N1).
    private static void Pristabell(IContainer e, List<ServiceavtaleLinje> linjer, ServiceavtaleAntall.Beregning b, HashSet<string> valg)
    {
        var kategorier = linjer.Where(l => !l.ErTillegg && l.Antall > 0 && l.Pris > 0).ToList();
        var tillegg = linjer.Where(l => l.ErTillegg).Select(t => (Linje: t, Ar: ServiceavtaleAntall.TilleggPerAr(t, b.Kategorier, b.Runder))).Where(x => x.Ar > 0).ToList();
        var visStk = valg.Contains(ServiceavtaleAntall.VisStk);
        var visRunde = valg.Contains(ServiceavtaleAntall.VisRunde);
        static string Kr(decimal v) => ServiceavtaleAntall.Kr(v);
        static string Tall(decimal v) => v.ToString("0.##", CultureInfo.GetCultureInfo("nb-NO"));
        static IContainer H(IContainer x) => x.BorderBottom(0.75f).BorderColor(RapportMal.Linje).PaddingVertical(4);
        static IContainer C(IContainer x) => x.BorderBottom(0.5f).BorderColor(RapportMal.Linje).PaddingVertical(4);
        static void Hode(IContainer x, string t) => x.Text(t.ToUpperInvariant()).FontSize(7).Bold().FontColor(RapportMal.Svak);

        e.Column(col =>
        {
            if (valg.Contains(ServiceavtaleAntall.VisLinjer) && kategorier.Count > 0)
            {
                col.Item().PaddingTop(6).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.ConstantColumn(50);
                        if (visStk) c.ConstantColumn(72);
                        if (visRunde) c.ConstantColumn(72);
                        c.ConstantColumn(78);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Element(H).Element(x => Hode(x, "Service"));
                        h.Cell().Element(H).AlignRight().Element(x => Hode(x, "Antall"));
                        if (visStk) h.Cell().Element(H).AlignRight().Element(x => Hode(x, "Per stk."));
                        if (visRunde) h.Cell().Element(H).AlignRight().Element(x => Hode(x, "Per runde"));
                        h.Cell().Element(H).AlignRight().Element(x => Hode(x, "Per år"));
                    });
                    foreach (var l in kategorier)
                    {
                        t.Cell().Element(C).Text(l.Navn);
                        t.Cell().Element(C).AlignRight().Text(Tall(l.Antall));
                        if (visStk) t.Cell().Element(C).AlignRight().Text(Kr(l.Pris));
                        if (visRunde) t.Cell().Element(C).AlignRight().Text(Kr(l.Antall * l.Pris));
                        t.Cell().Element(C).AlignRight().Text(Kr(l.Antall * l.Pris * b.Runder));
                    }
                });
            }

            if (tillegg.Count > 0)
            {
                col.Item().PaddingTop(10).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(3);
                        c.ConstantColumn(78);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Element(H).Element(x => Hode(x, "Tillegg"));
                        h.Cell().Element(H).Element(x => Hode(x, "Sats"));
                        h.Cell().Element(H).AlignRight().Element(x => Hode(x, "Per år"));
                    });
                    foreach (var (l, ar) in tillegg)
                    {
                        var sats = l.TilleggType switch
                        {
                            ServiceavtaleLinje.Prosent => $"{Tall(l.Pris)} % påslag",
                            ServiceavtaleLinje.PerKm => $"{Kr(l.Pris)} per km, {Tall(l.Antall)} km per runde",
                            ServiceavtaleLinje.PerAr => $"{Kr(l.Pris)} per år{(l.Antall != 1 ? $" × {Tall(l.Antall)}" : "")}",
                            _ => $"{Kr(l.Pris)} per runde{(l.Antall != 1 ? $" × {Tall(l.Antall)}" : "")}"
                        };
                        t.Cell().Element(C).Text(l.Navn);
                        t.Cell().Element(C).Text(sats).FontColor(RapportMal.Dempet);
                        t.Cell().Element(C).AlignRight().Text(Kr(ar));
                    }
                });
            }

            col.Item().PaddingTop(10).AlignRight().Width(250).Column(s =>
            {
                void Rad(string a, string v, bool fet = false, bool linje = false)
                {
                    var c = s.Item();
                    if (linje)
                    {
                        c = c.BorderTop(1.2f).BorderColor(PdfStil.Ink).PaddingTop(5);
                    }
                    c.Row(r =>
                    {
                        var ta = r.RelativeItem().Text(a);
                        var tv = r.AutoItem().Text(v);
                        if (fet)
                        {
                            ta.Bold().FontSize(11);
                            tv.Bold().FontSize(11);
                        }
                    });
                }
                Rad($"{b.Runder} servicerunder i året", Kr(b.Kategorier));
                if (b.Tillegg > 0) Rad("Tillegg", Kr(b.Tillegg));
                if (b.Rabatt > 0) Rad("Rabatt", "−" + Kr(b.Rabatt));
                Rad("Per år eks. mva", Kr(b.Arspris));
                Rad("Mva 25 %", Kr(b.Mva));
                Rad("Per år inkl. mva", Kr(b.InklMva), fet: true, linje: true);
                if (valg.Contains(ServiceavtaleAntall.VisMnd)) Rad("Per måned eks. mva", Kr(b.PerManed));
            });
        });
    }

    private static void Etikett(IContainer e, string tekst, Color farge) =>
        e.Text(tekst.ToUpperInvariant()).FontSize(7).Bold().FontColor(farge).LetterSpacing(0.08f);

    /// Deler avtaleteksten i seksjoner ved hver overskrift (h1-h3), slik at
    /// overskriften kan stå i venstre kolonne. Tekst før første overskrift
    /// blir en seksjon uten tittel. Tomme seksjoner hoppes over.
    public static List<Seksjon> Seksjoner(string? html)
    {
        var resultat = new List<Seksjon>();
        if (string.IsNullOrWhiteSpace(html))
        {
            return resultat;
        }

        using var dokument = new HtmlParser().ParseDocument($"<!doctype html><body>{html}</body>");
        string? tittel = null;
        var innhold = new System.Text.StringBuilder();

        void Avslutt()
        {
            if (innhold.Length > 0 && !string.IsNullOrWhiteSpace(innhold.ToString()))
            {
                resultat.Add(new Seksjon(tittel, innhold.ToString()));
            }
            else if (tittel is not null)
            {
                // Overskrift uten tekst under: vis den likevel, så ingenting forsvinner.
                resultat.Add(new Seksjon(tittel, ""));
            }
            innhold.Clear();
        }

        foreach (var node in dokument.Body!.ChildNodes)
        {
            if (node is AngleSharp.Dom.IElement el && el.TagName.ToLowerInvariant() is "h1" or "h2" or "h3")
            {
                Avslutt();
                tittel = string.IsNullOrWhiteSpace(el.TextContent) ? null : el.TextContent.Trim();
                continue;
            }

            innhold.Append(node is AngleSharp.Dom.IElement e ? e.OuterHtml : WebUtility.HtmlEncode(node.TextContent));
        }
        Avslutt();
        return resultat;
    }
}
