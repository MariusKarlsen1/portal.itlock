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

    public sealed record LestTegning(byte[]? Data, string ContentType, string? Feil);

    // Leser en opplastet tegningsfil. PDF gjøres om til PNG (første side), bilder brukes som de er.
    public static async Task<LestTegning> LesAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        if (file.Size > 20_000_000)
        {
            return new LestTegning(null, "", $"\"{file.Name}\" er for stor (maks 20 MB).");
        }

        using var buffer = new MemoryStream();
        await using (var stream = file.OpenReadStream(maxAllowedSize: 20_000_000))
        {
            await stream.CopyToAsync(buffer);
        }
        var raw = buffer.ToArray();
        var erPdf = file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        if (!erPdf)
        {
            return new LestTegning(raw, string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType, null);
        }

        try
        {
            using var bitmap = PDFtoImage.Conversion.ToImage(raw, page: 0, options: new PDFtoImage.RenderOptions(Dpi: 200));
            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 90);
            return new LestTegning(encoded.ToArray(), "image/png", null);
        }
        catch (Exception)
        {
            return new LestTegning(null, "", $"Klarte ikke å lese PDF-filen \"{file.Name}\". Prøv å laste opp den som bilde i stedet.");
        }
    }

    // Lagrer branntegningen til en plantegning. Finnes det en fra før, erstattes den.
    public static async Task LagreBranntegningAsync(ApplicationDbContext db, int plantegningId,
        string filnavn, string contentType, byte[] data)
    {
        var brann = await db.PlantegningBranntegninger.FirstOrDefaultAsync(b => b.PlantegningId == plantegningId);
        if (brann is null)
        {
            db.PlantegningBranntegninger.Add(new PlantegningBrann
            {
                PlantegningId = plantegningId,
                Filnavn = filnavn,
                ContentType = contentType,
                Data = data,
            });
        }
        else
        {
            brann.Filnavn = filnavn;
            brann.ContentType = contentType;
            brann.Data = data;
            brann.OpplastetDato = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

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
