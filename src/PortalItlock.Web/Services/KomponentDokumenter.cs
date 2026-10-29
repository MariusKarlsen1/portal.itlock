using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Henter monteringsanvisning, datablad eller FDV til en vare, enten
// hovedfila på varen (kilde "vare", id = varens id) eller et av de ekstra
// dokumentene (kilde "ekstra", id = dokumentets id). Brukes av Dok-fanen
// på prosjektet i montørappen.
public static class KomponentDokumenter
{
    public sealed record Fil(byte[] Data, string ContentType, string Filnavn)
    {
        public bool ErPdf => ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase);
        public bool ErBilde => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    public static readonly string[] Typer = ["montasjeblad", "datablad", "fdv"];

    public static string Navn(string type) => type switch
    {
        "montasjeblad" => "Monteringsanvisning",
        "datablad" => "Datablad",
        _ => "FDV"
    };

    // Adressen fila lastes ned fra i portalen.
    public static string Adresse(string type, string kilde, int id) =>
        kilde == "vare" ? $"/komponent/{id}/{type}" : $"/komponent/{type}dokument/{id}";

    public static async Task<Fil?> HentAsync(ApplicationDbContext db, string type, string kilde, int id)
    {
        if (kilde == "vare")
        {
            var k = await db.LastFilerAsync(db.Components.FirstOrDefaultAsync(c => c.Id == id));
            return (k, type) switch
            {
                ({ MontasjebladData: { } d }, "montasjeblad") => new Fil(d, k.MontasjebladContentType ?? "application/pdf", k.MontasjebladFilnavn ?? "Monteringsanvisning.pdf"),
                ({ DatabladData: { } d }, "datablad") => new Fil(d, k.DatabladContentType ?? "application/pdf", k.DatabladFilnavn ?? "Datablad.pdf"),
                ({ FdvData: { } d }, "fdv") => new Fil(d, k.FdvContentType ?? "application/pdf", k.FdvFilnavn ?? "FDV.pdf"),
                _ => null
            };
        }

        return type switch
        {
            "montasjeblad" => await db.ComponentMontasjebladDokumenter.Where(x => x.Id == id).Select(x => new Fil(x.Data, x.ContentType, x.Filnavn)).FirstOrDefaultAsync(),
            "datablad" => await db.ComponentDatabladDokumenter.Where(x => x.Id == id).Select(x => new Fil(x.Data, x.ContentType, x.Filnavn)).FirstOrDefaultAsync(),
            "fdv" => await db.ComponentFdvDokumenter.Where(x => x.Id == id).Select(x => new Fil(x.Data, x.ContentType, x.Filnavn)).FirstOrDefaultAsync(),
            _ => null
        };
    }
}
