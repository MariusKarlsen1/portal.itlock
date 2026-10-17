using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Felles datauthenting for rapportene som beregnes fra portalens egne
// ferdigmeldte arbeidsordre (Kunde, Produkt, Resultat, Kontoplan, Salg per
// kunde-sammenligningen og oversikten), slik at alle regner likt.
public static class RapportData
{
    public sealed record OrdreLinjer(Kunde? Kunde, DateTime Fullfort, List<ArbeidsordreOkonomiBeregner.OkonomiLinje> Linjer)
    {
        public decimal Sum => Linjer.Sum(l => l.Belop);
    }

    public static async Task<List<OrdreLinjer>> HentAsync(ApplicationDbContext db, DateTime fra, DateTime til, string? kundeSok = null)
    {
        var tilEksklusiv = til.Date.AddDays(1);
        var query = db.Arbeidsordre
            .Where(a => a.FullfortDato != null && a.FullfortDato >= fra.Date && a.FullfortDato < tilEksklusiv);

        if (!string.IsNullOrWhiteSpace(kundeSok))
        {
            query = query.Where(a =>
                (a.Kunde != null && a.Kunde.Navn.Contains(kundeSok))
                || (a.Prosjekt != null && a.Prosjekt.Kunde != null && a.Prosjekt.Kunde.Navn.Contains(kundeSok)));
        }

        var ordrer = await query
            .Include(a => a.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.Tilbud).ThenInclude(t => t!.Linjer).ThenInclude(l => l.Component).ThenInclude(c => c!.Inntektskonto)
            .Include(a => a.Varer).ThenInclude(v => v.Component).ThenInclude(c => c!.Inntektskonto)
            .AsSplitQuery()
            .ToListAsync();

        return ordrer
            .Select(a => new OrdreLinjer(a.Kunde ?? a.Prosjekt?.Kunde, a.FullfortDato!.Value, ArbeidsordreOkonomiBeregner.BeregnLinjer(a)))
            .ToList();
    }

    public static string Initialer(string? navn)
    {
        var deler = (navn ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return deler.Length switch
        {
            0 => "?",
            1 => deler[0][..Math.Min(2, deler[0].Length)].ToUpperInvariant(),
            _ => $"{deler[0][0]}{deler[^1][0]}".ToUpperInvariant()
        };
    }

    // CSV med semikolon (norsk Excel) og tall uten tusenskille.
    public static string Csv(IEnumerable<IEnumerable<object?>> rader)
    {
        var sb = new StringBuilder();
        foreach (var rad in rader)
        {
            sb.AppendLine(string.Join(";", rad.Select(Felt)));
        }

        return sb.ToString();
    }

    private static string Felt(object? verdi)
    {
        var tekst = verdi switch
        {
            null => "",
            decimal d => d.ToString("0.##", CultureInfo.GetCultureInfo("nb-NO")),
            DateTime dt => dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            _ => verdi.ToString() ?? ""
        };

        return tekst.IndexOfAny([';', '"', '\n']) >= 0 ? $"\"{tekst.Replace("\"", "\"\"")}\"" : tekst;
    }
}
