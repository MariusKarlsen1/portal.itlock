using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;

namespace PortalItlock.Web.Services;

// Servicerapport for en servicerunde, mal "Klassisk" (se RapportMal) - samme
// utseende som arbeidsrapporten: status og utført arbeid, anbefalinger, neste
// planlagte service, byttede deler per dør, sjekkliste, bilder og
// signaturlinjer, med organisasjonens egen logo, kontaktinfo og farge.
// Rapporten bruker malen «Sandbånd» uten egen forside; forsideteksten fra
// runden (Servicerunde.Forside) blir innledningen når den er valgt.
public class ServicerapportPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public async Task<byte[]?> GenerateAsync(int servicerundeId)
    {
        var runde = await db.Servicerunder
            .Include(r => r.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(r => r.UtfortAvBruker)
            .Include(r => r.Deler).ThenInclude(d => d.Dor)
            .Include(r => r.Sjekkpunkter)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == servicerundeId);

        if (runde is null)
        {
            return null;
        }

        runde.Media = await db.ServicerundeMedia
            .Where(m => m.ServicerundeId == servicerundeId && m.ContentType.StartsWith("image/"))
            .OrderBy(m => m.OpprettetDato)
            .ToListAsync();

        var org = RapportProfil.Fra(tenantContext.Current);
        var prosjekt = runde.Prosjekt;
        var kunde = prosjekt?.Kunde;
        var kontakt = !string.IsNullOrWhiteSpace(prosjekt?.Kontaktperson) ? prosjekt!.Kontaktperson : kunde?.Kontaktperson;
        var adresse = !string.IsNullOrWhiteSpace(prosjekt?.Adresse) ? prosjekt!.Adresse : kunde?.Adresse;
        var utfortAv = runde.UtfortAvBruker?.Navn;
        var erUtfort = !runde.ErPlanlagt;

        var document = Document.Create(doc =>
        {
            // Mal «Sandbånd»: kunde og nøkkeltall i båndet øverst, forsideteksten
            // fra runden (hvis valgt) som innledning, deretter selve rapporten.
            doc.Page(page =>
            {
                RapportMal.Side(page);
                page.Header().Column(col => RapportMal.Topp(col, org, pdfLogo, "Servicerapport", prosjekt?.Navn,
                    $"Nr. {runde.Id}", runde.Dato, erUtfort, "Planlagt",
                    ("Kunde", kunde?.Navn), ("Kontaktperson", kontakt), ("Adresse", adresse),
                    ("Utført av", utfortAv), ("Neste service", runde.NesteServiceDato?.ToString("dd.MM.yyyy"))));

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(14);

                    if (!string.IsNullOrWhiteSpace(runde.Forside))
                    {
                        col.Item().Column(c => ForsideRenderer.Render(c, runde.Forside));
                    }

                    RapportMal.Seksjon(col, "Status og utført arbeid", org, runde.StatusBeskrivelse);
                    RapportMal.Anbefalinger(col, org, runde.Anbefalinger);

                    RapportMal.Tabell(col, "Byttede deler", org, ["Dør", "Hva ble byttet", "Hva var galt"], [60, -2, -2],
                        runde.Deler.OrderBy(d => d.Dato).Select(d => new[] { d.Dor?.Dornummer ?? "-", d.Beskrivelse, d.Feil ?? "-" }),
                        sisteHoyre: false);

                    RapportMal.Sjekkliste(col, org, runde.Sjekkpunkter.OrderBy(p => p.Rekkefolge).Select(p => (p.Fullfort, p.Tekst)));
                    RapportMal.Bilder(col, org, runde.Media.Select(m => m.Data));
                    RapportMal.Signatur(col, utfortAv, runde.Dato, kontakt);
                });

                RapportMal.Bunn(page.Footer(), org);
            });
        });

        return document.GeneratePdf();
    }
}
