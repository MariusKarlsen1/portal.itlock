using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Lagrer en opplastet plantegning. Finnes det fra før en tegning med samme navn
// i prosjektet, blir den nye en ny revisjon av den: den gamle filen arkiveres
// som PlantegningRevisjon, og dører og utstyr blir stående der de var (posisjonene
// er lagret i prosent av tegningen).
public static class PlantegningOpplasting
{
    public sealed record Resultat(Plantegning Plantegning, bool Revidert);

    public static async Task<Resultat> LagreAsync(ApplicationDbContext db, int prosjektId, string navn,
        string? byggetrinn, string filnavn, string contentType, byte[] data)
    {
        var rent = navn.Trim();
        var eksisterende = (await db.Plantegninger.Where(p => p.ProsjektId == prosjektId).ToListAsync())
            .Where(p => string.Equals(p.Navn.Trim(), rent, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        if (eksisterende is not null)
        {
            await db.Entry(eksisterende).Reference(p => p.Fil).LoadAsync();
            db.PlantegningRevisjoner.Add(new PlantegningRevisjon
            {
                PlantegningId = eksisterende.Id,
                Revisjon = eksisterende.Revisjon,
                Filnavn = eksisterende.Filnavn,
                ContentType = eksisterende.ContentType,
                Data = eksisterende.Data,
                OpplastetDato = eksisterende.RevidertDato ?? eksisterende.OpprettetDato,
            });
            eksisterende.Data = data;
            eksisterende.Filnavn = filnavn;
            eksisterende.ContentType = contentType;
            eksisterende.Revisjon++;
            eksisterende.RevidertDato = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return new Resultat(eksisterende, true);
        }

        var ny = new Plantegning
        {
            ProsjektId = prosjektId,
            Navn = rent,
            Byggetrinn = string.IsNullOrWhiteSpace(byggetrinn) ? null : byggetrinn.Trim(),
            Filnavn = filnavn,
            ContentType = contentType,
            Data = data,
        };
        db.Plantegninger.Add(ny);
        await db.SaveChangesAsync();
        return new Resultat(ny, false);
    }
}
