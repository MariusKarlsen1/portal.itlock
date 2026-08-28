using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public enum LagretPdfEndringType { LagtTil, Fjernet, Endret }

public record LagretPdfEndring(string Navn, LagretPdfEndringType Type, int GammelAntall, int NyAntall, decimal? GammelPris, decimal? NyPris);

/// Retning: 1 = økt, -1 = redusert, 0 = ny/uten sammenligningsgrunnlag.
public record LagretPdfNokkeltallEndring(string Navn, string GammelVerdi, string NyVerdi, string DeltaTekst, int Retning);

public class LagretPdfService(ApplicationDbContext db)
{
    public async Task<List<LagretPdf>> HentAsync(string entityType, int entityId)
    {
        return await db.LagredePdfer
            .Where(p => p.EntityType == entityType && p.EntityId == entityId)
            .OrderByDescending(p => p.OpprettetDato)
            .ToListAsync();
    }

    public async Task<LagretPdf> LagreAsync(string entityType, int entityId, string navn, byte[] data,
        List<LagretPdfLinje>? linjer = null, List<LagretPdfNokkeltall>? nokkeltall = null)
    {
        var pdf = new LagretPdf
        {
            EntityType = entityType,
            EntityId = entityId,
            Navn = navn,
            Data = data,
            DataJson = linjer is null ? null : JsonSerializer.Serialize(linjer),
            NokkeltallJson = nokkeltall is null ? null : JsonSerializer.Serialize(nokkeltall)
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

    public static List<LagretPdfNokkeltallEndring> SammenlignNokkeltall(LagretPdf gammel, LagretPdf ny)
    {
        var gammelNokkeltall = string.IsNullOrEmpty(gammel.NokkeltallJson)
            ? []
            : JsonSerializer.Deserialize<List<LagretPdfNokkeltall>>(gammel.NokkeltallJson) ?? [];
        var nyNokkeltall = string.IsNullOrEmpty(ny.NokkeltallJson)
            ? []
            : JsonSerializer.Deserialize<List<LagretPdfNokkeltall>>(ny.NokkeltallJson) ?? [];

        var gammelPerNavn = gammelNokkeltall.ToDictionary(n => n.Navn);

        var endringer = new List<LagretPdfNokkeltallEndring>();

        foreach (var n in nyNokkeltall)
        {
            var harGammel = gammelPerNavn.TryGetValue(n.Navn, out var g);
            var gammelVerdi = harGammel ? g!.Verdi : "-";

            if (harGammel && g!.Verdi == n.Verdi)
            {
                continue;
            }

            if (!harGammel)
            {
                endringer.Add(new LagretPdfNokkeltallEndring(n.Navn, gammelVerdi, n.Verdi, "ny", 0));
                continue;
            }

            var harTallgrunnlag = n.Tall != 0 || g!.Tall != 0 || !string.IsNullOrEmpty(n.Enhet) || !string.IsNullOrEmpty(g.Enhet);
            if (!harTallgrunnlag)
            {
                // Eldre lagrede versjoner (før tall/enhet ble sporet) - vi vet verdien endret seg, men ikke retning/størrelse.
                endringer.Add(new LagretPdfNokkeltallEndring(n.Navn, gammelVerdi, n.Verdi, "endret", 0));
                continue;
            }

            var delta = n.Tall - g!.Tall;
            var retning = delta > 0 ? 1 : delta < 0 ? -1 : 0;
            endringer.Add(new LagretPdfNokkeltallEndring(n.Navn, gammelVerdi, n.Verdi, FormatDelta(delta, n.Enhet), retning));
        }

        return endringer;
    }

    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    private static string FormatDelta(decimal delta, string enhet)
    {
        if (delta == 0)
        {
            return "uendret";
        }

        var fortegn = delta > 0 ? "+" : "−";
        var format = enhet == "kr" ? "N0" : "N1";
        var tall = Math.Abs(delta).ToString(format, Kultur);
        return string.IsNullOrEmpty(enhet) ? $"{fortegn}{tall}" : $"{fortegn}{tall} {enhet}";
    }
}
