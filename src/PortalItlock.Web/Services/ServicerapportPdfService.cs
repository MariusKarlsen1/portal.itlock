using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;

namespace PortalItlock.Web.Services;

// Servicerapport for en servicerunde, mal "Klassisk" (se RapportMal) - samme
// utseende som arbeidsrapporten: status og utført arbeid, anbefalinger, neste
// planlagte service, byttede deler per dør, sjekkliste, bilder og
// signaturlinjer, med organisasjonens egen logo, kontaktinfo og farge.
// Rapporten starter alltid med en egen forside (RapportMal.Forside) med
// forsideteksten fra runden (Servicerunde.Forside) når den er valgt.
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

        void Topp(QuestPDF.Infrastructure.IContainer header) => header.Column(col =>
            RapportMal.Topp(col, org, pdfLogo, "Servicerapport", prosjekt?.Navn,
                $"Nr. {runde.Id}", runde.Dato, erUtfort, "Planlagt"));

        var document = Document.Create(doc =>
        {
            // Mal "Klassisk med forside": alltid en forside, med forsideteksten
            // fra runden (hvis valgt) under tittelen.
            var kundeLinje = string.Join(" · ", new[] { kunde?.Navn, adresse }.Where(s => !string.IsNullOrWhiteSpace(s)));
            doc.Page(page => RapportMal.Forside(page, org, pdfLogo,
                $"Servicerapport · {runde.Dato:dd.MM.yyyy}",
                prosjekt?.Navn ?? "Servicerapport",
                kundeLinje,
                $"Nr. {runde.Id}",
                runde.Forside,
                (erUtfort ? "Utført" : "Planlagt", runde.Dato.ToString("dd.MM.yyyy")),
                ("Utført av", utfortAv),
                ("Neste service", runde.NesteServiceDato?.ToString("dd.MM.yyyy"))));

            doc.Page(page =>
            {
                RapportMal.Side(page);
                Topp(page.Header());

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);

                    RapportMal.InfoBoks(col, org,
                        ("Kunde", kunde?.Navn), ("Kontaktperson", kontakt), ("Adresse", adresse),
                        ("Serviceavtale", prosjekt?.Navn), ("Utført", runde.Dato.ToString("dd.MM.yyyy")), ("Utført av", utfortAv));

                    RapportMal.Seksjon(col, "Status og utført arbeid", org, runde.StatusBeskrivelse);
                    RapportMal.Anbefalinger(col, org, runde.Anbefalinger);
                    RapportMal.Seksjon(col, "Neste planlagte service", org, runde.NesteServiceDato?.ToString("dd.MM.yyyy"));

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
