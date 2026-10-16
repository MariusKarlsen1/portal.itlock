using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Kundens svar på tilbud service (godkjent eller avslått) med dato - brukes
// av /tilbud-service/godkjent og /tilbud-service/avslatt til vinnerrate og
// til å vise når kunden svarte.
public static class TilbudServiceSvar
{
    public sealed record Svar(int TilbudId, bool Godkjent, DateTime Dato);

    public static async Task<List<Svar>> HentAsync(ApplicationDbContext db)
    {
        var tilbud = await db.Tilbud
            .Where(t => t.Prosjekt!.Status == ProsjektStatus.Serviceavtale
                && (t.Status == EndringsmeldingStatus.Godkjent || t.Status == EndringsmeldingStatus.Avslatt))
            .Select(t => new { t.Id, t.Status, t.AvslattDato, t.OppdatertDato, t.OpprettetDato })
            .ToListAsync();

        // Godkjent-dato finnes ikke som eget felt - den hentes fra siste
        // statusendring til "Godkjent" i tilbudets logg (TilbudSkjema/StatusEndret).
        var godkjentIder = tilbud.Where(t => t.Status == EndringsmeldingStatus.Godkjent).Select(t => t.Id).ToList();
        var tekst = $"til \"{EndringsmeldingStatus.Godkjent.Visningsnavn()}\"";
        var godkjentDato = await db.TilbudHendelser
            .Where(h => godkjentIder.Contains(h.TilbudId) && h.Beskrivelse.Contains(tekst))
            .GroupBy(h => h.TilbudId)
            .Select(g => new { TilbudId = g.Key, Dato = g.Max(h => h.Tidspunkt) })
            .ToDictionaryAsync(x => x.TilbudId, x => x.Dato);

        return tilbud.Select(t => t.Status == EndringsmeldingStatus.Godkjent
                ? new Svar(t.Id, true, godkjentDato.TryGetValue(t.Id, out var d) ? d : (t.OppdatertDato ?? t.OpprettetDato).ToLocalTime())
                : new Svar(t.Id, false, (t.AvslattDato ?? t.OppdatertDato ?? t.OpprettetDato).ToLocalTime()))
            .ToList();
    }

    // Andel godkjent av alle besvarte tilbud (valgt år, eller alle år).
    public static (int Godkjent, int Besvart) Vinnerrate(List<Svar> svar, int? ar)
    {
        var aktuelle = svar.Where(s => ar is null || s.Dato.Year == ar).ToList();
        return (aktuelle.Count(s => s.Godkjent), aktuelle.Count);
    }

    // Sum eks. mva: det kunden fikk (SendtSum), ellers regnet som på tilbudssiden.
    public static async Task<Dictionary<int, decimal>> BeregnSummerAsync(ApplicationDbContext db, IEnumerable<int> tilbudUtenSendtSum)
    {
        var ider = tilbudUtenSendtSum.ToList();
        if (ider.Count == 0)
        {
            return [];
        }

        return (await db.Tilbud.Where(t => ider.Contains(t.Id)).Include(t => t.Linjer).ToListAsync())
            .ToDictionary(t => t.Id, TilbudBeregningHelper.BeregnTotaltUtenMva);
    }
}
