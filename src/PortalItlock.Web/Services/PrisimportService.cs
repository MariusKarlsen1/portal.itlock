using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public class PrisimportService(ApplicationDbContext db)
{
    public sealed class ArkKolonne
    {
        public int Indeks { get; set; }
        public string Overskrift { get; set; } = "";
    }

    public sealed class ImportRad
    {
        public int RadNummer { get; set; }
        public string? Produktkode { get; set; }
        public string? Navn { get; set; }
        public string? Enhet { get; set; }
        public decimal? PrisNetto { get; set; }
        public decimal? PrisVeiledende { get; set; }
        public bool ErNyVare { get; set; }
        public int? EksisterendeComponentId { get; set; }
        public string? EksisterendeNavn { get; set; }
        public string? RabattgruppeKode { get; set; }
        public bool NettoErBeregnet { get; set; }
        public string? Feil { get; set; }
        public bool Inkluder { get; set; } = true;
    }

    public (List<ArkKolonne> Kolonner, List<string[]> Rader) LesFil(Stream fil)
    {
        using var wb = new XLWorkbook(fil);
        var ws = wb.Worksheets.First();
        var brukt = ws.RangeUsed();
        if (brukt is null)
        {
            return ([], []);
        }

        var rader = brukt.RowsUsed().ToList();
        if (rader.Count == 0)
        {
            return ([], []);
        }

        var forsteRad = rader[0];
        var antallKolonner = forsteRad.CellsUsed().Count();
        var kolonner = Enumerable.Range(0, antallKolonner)
            .Select(i => new ArkKolonne { Indeks = i, Overskrift = forsteRad.Cell(i + 1).GetString().Trim() })
            .ToList();

        var dataRader = rader.Skip(1)
            .Select(r => Enumerable.Range(0, antallKolonner).Select(i => r.Cell(i + 1).GetString().Trim()).ToArray())
            .ToList();

        return (kolonner, dataRader);
    }

    public async Task<List<ImportRad>> ForhandsvisAsync(
        List<string[]> rader, string leverandor,
        int produktkodeKol, int? navnKol, int? enhetKol, int? prisNettoKol, int? prisVeiledendeKol)
    {
        var eksisterende = await db.Components
            .Include(c => c.Rabattgruppe)
            .Where(c => c.Leverandor != null && c.Leverandor.ToLower() == leverandor.ToLower() && c.Produktkode != null)
            .ToListAsync();
        var eksisterendePerKode = eksisterende
            .GroupBy(c => c.Produktkode!.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var resultat = new List<ImportRad>();
        for (var i = 0; i < rader.Count; i++)
        {
            var rad = rader[i];
            var produktkode = HentFelt(rad, produktkodeKol);
            var navn = HentFelt(rad, navnKol);

            if (string.IsNullOrWhiteSpace(produktkode) && string.IsNullOrWhiteSpace(navn))
            {
                continue;
            }

            var importRad = new ImportRad
            {
                RadNummer = i + 2,
                Produktkode = produktkode,
                Navn = navn,
                Enhet = HentFelt(rad, enhetKol),
                PrisNetto = ParsePris(HentFelt(rad, prisNettoKol)),
                PrisVeiledende = ParsePris(HentFelt(rad, prisVeiledendeKol))
            };

            if (string.IsNullOrWhiteSpace(produktkode))
            {
                importRad.Feil = "Mangler produktkode";
            }
            else if (eksisterendePerKode.TryGetValue(produktkode.Trim().ToLowerInvariant(), out var funnet))
            {
                importRad.EksisterendeComponentId = funnet.Id;
                importRad.EksisterendeNavn = funnet.Navn;
                importRad.ErNyVare = false;

                if (funnet.Rabattgruppe is not null && importRad.PrisVeiledende.HasValue)
                {
                    importRad.RabattgruppeKode = funnet.Rabattgruppe.Kode;
                    importRad.PrisNetto = Math.Round(importRad.PrisVeiledende.Value * (1 - funnet.Rabattgruppe.RabattProsent / 100m), 2);
                    importRad.NettoErBeregnet = true;
                }
            }
            else
            {
                importRad.ErNyVare = true;
                if (string.IsNullOrWhiteSpace(navn))
                {
                    importRad.Feil = "Ny vare mangler navn";
                }
            }

            resultat.Add(importRad);
        }

        return resultat;
    }

    public async Task<(int Oppdatert, int Nye)> ImporterAsync(List<ImportRad> rader, string leverandor)
    {
        var oppdatert = 0;
        var nye = 0;

        foreach (var rad in rader.Where(r => r.Inkluder && r.Feil is null))
        {
            if (rad.EksisterendeComponentId.HasValue)
            {
                var comp = await db.Components.FindAsync(rad.EksisterendeComponentId.Value);
                if (comp is null)
                {
                    continue;
                }

                var nyNetto = rad.PrisNetto ?? comp.PrisNetto;
                var nyVeil = rad.PrisVeiledende ?? comp.PrisVeiledende;
                PrisHistorikkLogger.Logg(db, comp, nyNetto, nyVeil, $"Prisimport ({leverandor})");

                comp.PrisNetto = nyNetto;
                comp.PrisVeiledende = nyVeil;

                oppdatert++;
            }
            else
            {
                db.Components.Add(new Component
                {
                    Navn = rad.Navn!,
                    Produktkode = rad.Produktkode,
                    Leverandor = leverandor,
                    Enhet = string.IsNullOrWhiteSpace(rad.Enhet) ? null : rad.Enhet,
                    PrisNetto = rad.PrisNetto,
                    PrisVeiledende = rad.PrisVeiledende
                });
                nye++;
            }
        }

        await db.SaveChangesAsync();
        return (oppdatert, nye);
    }

    private static string? HentFelt(string[] rad, int? indeks) =>
        indeks.HasValue && indeks.Value >= 0 && indeks.Value < rad.Length ? rad[indeks.Value] : null;

    private static decimal? ParsePris(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return null;
        }

        var rens = tekst.Replace("kr", "", StringComparison.OrdinalIgnoreCase).Replace(",-", "").Replace(" ", "").Trim();
        if (rens.Length == 0)
        {
            return null;
        }

        if (decimal.TryParse(rens, NumberStyles.Any, CultureInfo.GetCultureInfo("nb-NO"), out var verdiNb))
        {
            return verdiNb;
        }

        if (decimal.TryParse(rens, NumberStyles.Any, CultureInfo.InvariantCulture, out var verdiInv))
        {
            return verdiInv;
        }

        return null;
    }
}
