using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public enum LagretPdfEndringType { LagtTil, Fjernet, Endret }

public record LagretPdfEndring(string Navn, LagretPdfEndringType Type, int GammelAntall, int NyAntall, decimal? GammelPris, decimal? NyPris);

public class LagretPdfService(ApplicationDbContext db)
{
    public async Task<List<LagretPdf>> HentAsync(string entityType, int entityId)
    {
        return await db.LagredePdfer
            .Where(p => p.EntityType == entityType && p.EntityId == entityId)
            .OrderByDescending(p => p.OpprettetDato)
            .ToListAsync();
    }

    public async Task<LagretPdf> LagreAsync(string entityType, int entityId, string navn, byte[] data, List<LagretPdfLinje>? linjer = null)
    {
        var pdf = new LagretPdf
        {
            EntityType = entityType,
            EntityId = entityId,
            Navn = navn,
            Data = data,
            DataJson = linjer is null ? null : JsonSerializer.Serialize(linjer)
        };

        db.LagredePdfer.Add(pdf);
        await db.SaveChangesAsync();
        return pdf;
    }

    public static List<LagretPdfEndring> Sammenlign(LagretPdf gammel, LagretPdf ny)
    {
        var gammelLinjer = string.IsNullOrEmpty(gammel.DataJson)
            ? []
            : JsonSerializer.Deserialize<List<LagretPdfLinje>>(gammel.DataJson) ?? [];
        var nyLinjer = string.IsNullOrEmpty(ny.DataJson)
            ? []
            : JsonSerializer.Deserialize<List<LagretPdfLinje>>(ny.DataJson) ?? [];

        var gammelPerNavn = gammelLinjer.ToDictionary(l => l.Navn);
        var nyPerNavn = nyLinjer.ToDictionary(l => l.Navn);

        var endringer = new List<LagretPdfEndring>();

        foreach (var navn in gammelPerNavn.Keys.Union(nyPerNavn.Keys).OrderBy(n => n))
        {
            var harGammel = gammelPerNavn.TryGetValue(navn, out var g);
            var harNy = nyPerNavn.TryGetValue(navn, out var n);

            if (harGammel && !harNy)
            {
                endringer.Add(new LagretPdfEndring(navn, LagretPdfEndringType.Fjernet, g!.Antall, 0, g.Pris, null));
            }
            else if (!harGammel && harNy)
            {
                endringer.Add(new LagretPdfEndring(navn, LagretPdfEndringType.LagtTil, 0, n!.Antall, null, n.Pris));
            }
            else if (g!.Antall != n!.Antall || g.Pris != n.Pris)
            {
                endringer.Add(new LagretPdfEndring(navn, LagretPdfEndringType.Endret, g.Antall, n.Antall, g.Pris, n.Pris));
            }
        }

        return endringer;
    }
}
