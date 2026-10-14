using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;

namespace PortalItlock.Web.Services;

// Arbeidsrapport, mal "Klassisk" (se RapportMal): oppdraget, utført arbeid,
// anbefalinger, sjekkliste, materiell, bilder og signaturlinjer, med
// organisasjonens egen logo, kontaktinfo og farge. Priser og timer vises
// aldri. Tomme felt og seksjoner utelates.
public class ArbeidsordrePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public async Task<byte[]?> GenerateAsync(int arbeidsordreId)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.AnsvarligMontor)
            .Include(a => a.FullfortAvBruker)
            .Include(a => a.Sjekkpunkter)
            .Include(a => a.Varer)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == arbeidsordreId);

        if (ordre is null)
        {
            return null;
        }

        ordre.Media = await db.ArbeidsordreMedia
            .Where(m => m.ArbeidsordreId == arbeidsordreId && m.ContentType.StartsWith("image/"))
            .OrderBy(m => m.OpprettetDato)
            .ToListAsync();

        // Materiell: varelinjene fra tilbudet ordren er laget fra (det montøren
        // plukker), pluss varer lagt inn direkte på ordren. Kun navn og antall.
        var materiell = new List<string[]>();
        if (ordre.TilbudId.HasValue)
        {
            var linjer = await db.TilbudLinjer
                .Where(l => l.TilbudId == ordre.TilbudId.Value && !l.ErGruppering && l.LevertAv == LevertAv.F)
                .OrderBy(l => l.Rekkefolge)
                .ToListAsync();
            materiell.AddRange(linjer
                .Where(l => !ErArbeidslinje(l))
                .Select(l => new[] { l.Navn, $"{l.Antall} {(string.IsNullOrWhiteSpace(l.Enhet) ? "stk" : l.Enhet)}" }));
        }
        materiell.AddRange(ordre.Varer.OrderBy(v => v.Navn).Select(v => new[] { v.Navn, $"{v.Antall} stk" }));

        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = ordre.Kunde ?? ordre.Prosjekt?.Kunde;
        var kontakt = !string.IsNullOrWhiteSpace(ordre.Prosjekt?.Kontaktperson) ? ordre.Prosjekt!.Kontaktperson : kunde?.Kontaktperson;
        var adresse = string.Join(", ", new[]
        {
            ordre.Adresse,
            string.Join(" ", new[] { ordre.Postnr, ordre.Sted }.Where(s => !string.IsNullOrWhiteSpace(s)))
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(adresse))
        {
            adresse = ordre.Prosjekt?.Adresse ?? kunde?.Adresse ?? "";
        }

        var utfortDato = ordre.FullfortDato ?? ordre.PlanlagtDato;
        var utfortAv = ordre.FullfortAvBruker?.Navn ?? ordre.AnsvarligMontor?.Navn;
        var erUtfort = ordre.Status is ArbeidsordreStatus.Ferdig or ArbeidsordreStatus.Fakturert;

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                RapportMal.Side(page);

                page.Header().Column(col => RapportMal.Topp(col, org, pdfLogo, "Arbeidsrapport", ordre.Tittel,
                    $"Nr. {ordre.Id}", utfortDato ?? DateTime.Today, erUtfort, ordre.Status.Visningsnavn()));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);

                    RapportMal.InfoBoks(col, org,
                        ("Kunde", kunde?.Navn), ("Kontaktperson", kontakt), ("Adresse", adresse),
                        ("Prosjekt", ordre.Prosjekt?.Navn), ("Utført", utfortDato?.ToString("dd.MM.yyyy")), ("Montør", utfortAv));

                    RapportMal.Seksjon(col, "Oppdraget", org, ordre.Beskrivelse);
                    RapportMal.Seksjon(col, "Utført arbeid", org, ordre.UtfortArbeid);
                    RapportMal.Anbefalinger(col, org, ordre.Anbefalinger);
                    RapportMal.Sjekkliste(col, org, ordre.Sjekkpunkter.OrderBy(p => p.Rekkefolge).Select(p => (p.Fullfort, p.Tekst)));
                    RapportMal.Tabell(col, "Materiell brukt", org, ["Materiell", "Antall"], [-1, 70], materiell, sisteHoyre: true);
                    RapportMal.Bilder(col, org, ordre.Media.Select(m => m.Data));
                    RapportMal.Signatur(col, utfortAv, utfortDato, kontakt);
                });

                RapportMal.Bunn(page.Footer(), org);
            });
        });

        return document.GeneratePdf();
    }

    // Arbeid (montasje, timer, kjøring) er ikke materiell - og timer skal ikke
    // stå i rapporten.
    private static readonly string[] ArbeidsEnheter = ["t", "time", "timer", "rs", "km"];
    private static readonly string[] ArbeidsOrd = ["montasje", "montering", "arbeid", "kjøring", "reise", "time"];

    private static bool ErArbeidslinje(TilbudLinje l) =>
        l.ComponentId is null
        && (ArbeidsEnheter.Contains((l.Enhet ?? "").Trim().ToLowerInvariant())
            || ArbeidsOrd.Any(o => l.Navn.Contains(o, StringComparison.OrdinalIgnoreCase)));
}
