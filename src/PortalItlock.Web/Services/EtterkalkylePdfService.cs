using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Etterkalkyle som PDF: slik gikk det, faktisk mot kalkulert som i
// økonomiarket, per arbeidsordre, og gjennomgangen (årsak, kommentar og
// spørsmålene) hvis prosjektet er kontrollert.
public class EtterkalkylePdfService(ApplicationDbContext db, ProsjektOkonomiService okonomi, ProsjektOppfolgingService oppfolging, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    private static string Kr(decimal v) => v.ToString("#,##0", Kultur) + " kr";
    private static string T(decimal v) => v.ToString("0.#", Kultur) + " t";
    private static string DG(decimal? v) => v is { } d ? (d * 100).ToString("0.#", Kultur) + " %" : "–";

    private static string Avvik(decimal v, decimal mot)
    {
        if (mot <= 0) return "–";
        var p = (v / mot - 1) * 100;
        return (p > 0 ? "+" : "") + p.ToString("0", Kultur) + " %";
    }

    public async Task<byte[]?> GenerateAsync(int prosjektId)
    {
        var prosjekt = await db.Prosjekter.AsNoTracking().Include(p => p.Kunde).Include(p => p.Prosjektleder).FirstOrDefaultAsync(p => p.Id == prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var o = await okonomi.BeregnAsync(prosjektId);
        var kontroll = await db.ProsjektEtterkalkyler.AsNoTracking().FirstOrDefaultAsync(k => k.ProsjektId == prosjektId && !k.ErUtkast);
        var ek = await oppfolging.EtterkalkyleAsync(prosjektId);
        var org = RapportProfil.Fra(tenantContext.Current);

        var doc = Document.Create(d =>
        {
            d.Page(page =>
            {
                RapportMal.Side(page);
                page.Header().Column(col => RapportMal.Topp(col, org, pdfLogo, "Etterkalkyle", prosjekt.Navn,
                    $"P-{prosjekt.Id}", kontroll?.Kontrollert ?? DateTime.Now, kontroll is not null, "Ikke kontrollert"));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);

                    RapportMal.InfoBoks(col, org,
                        ("Kunde", prosjekt.Kunde?.Navn),
                        ("Prosjektleder", prosjekt.Prosjektleder?.Navn),
                        ("Overlevert", prosjekt.OverlevertDato?.ToString("dd.MM.yyyy")),
                        ("Tilbudt (inntekt)", Kr(o.Inntekt)),
                        ("Dekningsgrad", $"{DG(o.FaktiskDekningsgrad)} (tilbudt {DG(o.BudsjettDekningsgrad)})"),
                        ("Fremdrift", o.FremdriftTekst));

                    if (ek is not null)
                    {
                        string Fortegn(decimal v) => Math.Abs(v) < 1 ? "0" : (v > 0 ? "+" : "−") + Math.Abs(v).ToString("#,##0", Kultur);
                        RapportMal.Seksjon(col, $"Slik gikk det: {ek.DomTekst.ToLowerInvariant()}", org, ek.Forklaring);
                        var rader = new List<string[]> { new[] { "Kontrakt", Kr(ek.Kontrakt), Kr(ek.Kontrakt), "0" } };
                        if (ek.Endringer != 0) rader.Add(["Endringer og tilvalg", Kr(ek.Endringer), Kr(ek.Endringer), "0"]);
                        if (ek.Indeks != 0) rader.Add(["Indeksregulering", Kr(0), Kr(ek.Indeks), Fortegn(ek.Indeks)]);
                        rader.Add(["Materiell" + (ek.VarerAnslatt ? " (anslått)" : ""), Kr(ek.KalkVarer), Kr(ek.FaktiskVarer), Fortegn(ek.VarerAvvik)]);
                        rader.Add(["Lønn", Kr(ek.KalkLonn), Kr(ek.FaktiskLonn), Fortegn(ek.LonnAvvik)]);
                        rader.Add(["Resultat", Kr(ek.KalkResultat), Kr(ek.FaktiskResultat), Fortegn(ek.ResultatAvvik)]);
                        rader.Add(["Dekningsgrad", DG(ek.KalkDG), DG(ek.FaktiskDG), $"{(ek.DgPoeng >= 0 ? "+" : "−")}{Math.Abs(ek.DgPoeng).ToString("0.0", Kultur)} poeng"]);
                        RapportMal.Tabell(col, "Faktisk mot kalkulert", org, ["", "Kalkyle", "Faktisk", "Avvik"], [-2, -1, -1, 80], rader, sisteHoyre: true);
                        RapportMal.Tabell(col, "Hvor ble avviket av?", org, ["", "Beløp"], [-3, -1],
                            ek.Bidrag.Select(b => new[] { b.Tekst, b.Total ? Kr(b.Belop) : Fortegn(b.Belop) }), sisteHoyre: true);
                    }

                    if (ek is null)
                    {
                        RapportMal.Tabell(col, "Tilbudt mot faktisk", org,
                            ["", "Tilbudt", "Faktisk", "Avvik"], [-2, -1, -1, 70],
                            [
                                ["Timer", T(o.BudsjettTimer), T(o.FaktiskTimer), Avvik(o.FaktiskTimer, o.BudsjettTimer)],
                                ["Timekost", Kr(o.BudsjettTimekost), Kr(o.FaktiskTimekost), Avvik(o.FaktiskTimekost, o.BudsjettTimekost)],
                                ["Varer", Kr(o.BudsjettVarer), Kr(o.FaktiskVarer), Avvik(o.FaktiskVarer, o.BudsjettVarer)],
                                ["Total kost", Kr(o.BudsjettKost), Kr(o.FaktiskKost), Avvik(o.FaktiskKost, o.BudsjettKost)],
                                ["Resultat (inntekt − kost)", Kr(o.Inntekt - o.BudsjettKost), Kr(o.Inntekt - o.FaktiskKost), ""],
                            ], sisteHoyre: true);
                    }

                    if (o.Arbeidsordre.Count > 0)
                    {
                        RapportMal.Tabell(col, "Per arbeidsordre", org,
                            ["Arbeidsordre", "Status", "Timer", "Timekost", "Varer"], [-3, -1, 50, 70, 70],
                            o.Arbeidsordre.Select(a => new[] { a.Tittel, a.Status.Visningsnavn(), T(a.Timer), Kr(a.Timekost), a.Varer != 0 ? Kr(a.Varer) : "–" }),
                            sisteHoyre: true);
                    }

                    RapportMal.Tabell(col, "Grunnlag", org,
                        ["Tilbud", "Type", "Budsjett låst", "Tilbudt"], [-3, 80, 80, 80],
                        o.Tilbud.Select(t => new[] { t.Tittel, t.ErEndring ? "Endringsmelding" : "Tilbud", t.Laast.ToString("dd.MM.yyyy"), Kr(t.Salgssum) }),
                        sisteHoyre: true);

                    if (kontroll is not null)
                    {
                        RapportMal.Seksjon(col, "Gjennomgang", org,
                            $"Kontrollert {kontroll.Kontrollert:dd.MM.yyyy} av {kontroll.KontrollertAv ?? "–"}. Årsak: {kontroll.Arsak.Visningsnavn()}."
                            + (string.IsNullOrWhiteSpace(kontroll.Kommentar) ? "" : $"\n{kontroll.Kommentar}"));
                        var svar = new[]
                        {
                            ("Hva gikk bra?", kontroll.GikkBra), ("Hva gikk dårlig?", kontroll.GikkDarlig), ("Hva må vi forbedre?", kontroll.Forbedre),
                            ("Forsinkelser, egne forhold", kontroll.ForsinkelseEgne), ("Forsinkelser, andre", kontroll.ForsinkelseAndre)
                        }.Where(x => !string.IsNullOrWhiteSpace(x.Item2)).ToList();
                        if (svar.Count > 0)
                        {
                            RapportMal.Tabell(col, "Kommentarer og tiltak", org, ["Spørsmål", "Svar"], [-1, -2],
                                svar.Select(x => new[] { x.Item1, x.Item2! }), sisteHoyre: false);
                        }
                    }

                    col.Item().Text(o.VarerFraLager
                        ? "Varer er faktisk forbruk fra lageret. Timekost er timer × intern timekost."
                        : "Varer er anslått fra tilbudets varer × fremdrift. Timekost er timer × intern timekost.")
                        .FontSize(8).FontColor(RapportMal.Svak);
                });

                page.Footer().Element(e => RapportMal.Bunn(e, org));
            });
        });

        return doc.GeneratePdf();
    }
}
