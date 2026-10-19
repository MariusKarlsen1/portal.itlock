using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Driftsmeldinger fra vaktmestere havner i Forespørsler (egen side for driftsmeldinger
// er fjernet). Hver driftsmelding får én forespørsel med kanal «Driftsportal» som
// peker tilbake på meldingen (dør, prosjekt, kategori, bilder/video).
public static class DriftsmeldingForesporsel
{
    // Lager forespørsler for driftsmeldinger som ikke har fått det ennå - nye og eldre.
    public static async Task SynkAsync(ApplicationDbContext db)
    {
        var harForesporsel = db.Foresporsler.Where(f => f.DriftsmeldingId != null).Select(f => f.DriftsmeldingId);
        var mangler = await db.Driftsmeldinger
            .Where(m => !harForesporsel.Contains(m.Id))
            .Include(m => m.Dor).ThenInclude(d => d!.Prosjekt)
            .Include(m => m.InnmeldtAvBruker)
            .AsNoTracking()
            .ToListAsync();

        if (mangler.Count == 0)
        {
            return;
        }

        foreach (var m in mangler)
        {
            db.Foresporsler.Add(Lag(m));
        }

        await db.SaveChangesAsync();
    }

    public static string Emne(Driftsmelding m)
    {
        var hva = !string.IsNullOrWhiteSpace(m.Tittel) ? m.Tittel!.Trim()
            : !string.IsNullOrWhiteSpace(m.Kategori) ? m.Kategori!.Trim()
            : "Driftsmelding";
        var dor = m.Dor is null ? "" : $" – dør {m.Dor.Dornummer}";
        var prosjekt = m.Dor?.Prosjekt is null ? "" : $" ({m.Dor.Prosjekt.Navn})";
        return $"{(m.Haster ? "Haster: " : "")}{hva}{dor}{prosjekt}";
    }

    private static Foresporsel Lag(Driftsmelding m) => new()
    {
        Kanal = ForesporselKanal.Driftsportal,
        DriftsmeldingId = m.Id,
        FraNavn = m.InnmeldtAvBruker?.Navn ?? "Vaktmester",
        FraEpost = m.InnmeldtAvBruker?.Epost ?? "",
        Emne = Emne(m),
        Innhold = m.Tekst,
        MottattDato = m.OpprettetDato,
        Lest = m.LestAvAnsatt
    };
}
