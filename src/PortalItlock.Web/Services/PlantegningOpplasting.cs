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

    // «Tegning (2).pdf» fra nedlastinger er samme tegning som «Tegning.pdf».
    public static string UtenKopiNummer(string navn) =>
        System.Text.RegularExpressions.Regex.Replace(navn, @"\s*\(\d{1,3}\)\s*$", "").Trim();

    // Sammenligner navn uten store/små bokstaver, ekstra mellomrom og kopinummer.
    public static string Nokkel(string navn) =>
        System.Text.RegularExpressions.Regex.Replace(UtenKopiNummer(TekstReparasjon.Fiks(navn).Trim()), @"\s+", " ").ToLowerInvariant();

    // Ny revisjon av en bestemt tegning: den gjeldende filen arkiveres som
    // revisjon, den nye lagres akkurat som den er, og dører og utstyr blir
    // stående (posisjonene er i prosent av tegningen).
    public static async Task NyRevisjonAsync(ApplicationDbContext db, Plantegning tegning,
        string filnavn, string contentType, byte[] data)
    {
        await db.Entry(tegning).Reference(p => p.Fil).LoadAsync();
        db.PlantegningRevisjoner.Add(new PlantegningRevisjon
        {
            PlantegningId = tegning.Id,
            Revisjon = tegning.Revisjon,
            Filnavn = tegning.Filnavn,
            ContentType = tegning.ContentType,
            Data = tegning.Data,
            OpplastetDato = tegning.RevidertDato ?? tegning.OpprettetDato,
        });
        tegning.Data = data;
        tegning.Filnavn = filnavn;
        tegning.ContentType = contentType;
        tegning.Revisjon++;
        tegning.RevidertDato = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public static async Task<Resultat> LagreAsync(ApplicationDbContext db, int prosjektId, string navn,
        string? byggetrinn, string filnavn, string contentType, byte[] data)
    {
        var rent = UtenKopiNummer(TekstReparasjon.Fiks(navn).Trim());
        var nokkel = Nokkel(rent);
        var eksisterende = (await db.Plantegninger.Where(p => p.ProsjektId == prosjektId).ToListAsync())
            .Where(p => Nokkel(p.Navn) == nokkel)
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        if (eksisterende is not null)
        {
            await NyRevisjonAsync(db, eksisterende, filnavn, contentType, data);
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
