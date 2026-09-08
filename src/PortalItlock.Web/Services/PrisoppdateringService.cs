using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Aarlig prisoppdatering fra leverandoer: matcher kun paa produktkode eller GTIN/EAN
// mot eksisterende varer (oppretter ALDRI nye), og lar brukeren velge en fremtidig dato
// prisendringen skal gjelde fra - i motsetning til Prisimport som endrer prisen med en gang.
public class PrisoppdateringService(ApplicationDbContext db)
{
    public sealed class OppdateringRad
    {
        public int RadNummer { get; set; }
        public string? Produktkode { get; set; }
        public string? Gtin { get; set; }
        public decimal? NyPrisNetto { get; set; }
        public decimal? NyPrisVeiledende { get; set; }

        public int? ComponentId { get; set; }
        public string? ComponentNavn { get; set; }
        public decimal? GammelPrisNetto { get; set; }
        public decimal? GammelPrisVeiledende { get; set; }

        public bool ErEndret { get; set; }
        public string? Feil { get; set; }
        public bool Inkluder { get; set; } = true;
    }

    public async Task<List<OppdateringRad>> ForhandsvisAsync(
        List<string[]> rader,
        int? produktkodeKol, int? gtinKol, int? prisNettoKol, int? prisVeiledendeKol)
    {
        var alle = await db.Components.ToListAsync();
        var perProduktkode = alle
            .Where(c => !string.IsNullOrWhiteSpace(c.Produktkode))
            .GroupBy(c => c.Produktkode!.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var perGtin = alle
            .Where(c => !string.IsNullOrWhiteSpace(c.Gtin))
            .GroupBy(c => c.Gtin!.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var resultat = new List<OppdateringRad>();
        for (var i = 0; i < rader.Count; i++)
        {
            var rad = rader[i];
            var produktkode = HentFelt(rad, produktkodeKol);
            var gtin = HentFelt(rad, gtinKol);

            if (string.IsNullOrWhiteSpace(produktkode) && string.IsNullOrWhiteSpace(gtin))
            {
                continue;
            }

            var oppdRad = new OppdateringRad
            {
                RadNummer = i + 2,
                Produktkode = produktkode,
                Gtin = gtin,
                NyPrisNetto = ParsePris(HentFelt(rad, prisNettoKol)),
                NyPrisVeiledende = ParsePris(HentFelt(rad, prisVeiledendeKol))
            };

            Component? funnet = null;
            if (!string.IsNullOrWhiteSpace(produktkode))
            {
                perProduktkode.TryGetValue(produktkode.Trim().ToLowerInvariant(), out funnet);
            }
            if (funnet is null && !string.IsNullOrWhiteSpace(gtin))
            {
                perGtin.TryGetValue(gtin.Trim().ToLowerInvariant(), out funnet);
            }

            if (funnet is null)
            {
                oppdRad.Feil = "Fant ikke vare på produktkode/GTIN";
            }
            else
            {
                oppdRad.ComponentId = funnet.Id;
                oppdRad.ComponentNavn = funnet.Navn;
                oppdRad.GammelPrisNetto = funnet.PrisNetto;
                oppdRad.GammelPrisVeiledende = funnet.PrisVeiledende;

                var nettoEndret = oppdRad.NyPrisNetto.HasValue && oppdRad.NyPrisNetto != funnet.PrisNetto;
                var veilEndret = oppdRad.NyPrisVeiledende.HasValue && oppdRad.NyPrisVeiledende != funnet.PrisVeiledende;
                oppdRad.ErEndret = nettoEndret || veilEndret;

                if (!oppdRad.NyPrisNetto.HasValue)
                {
                    oppdRad.NyPrisNetto = funnet.PrisNetto;
                }
                if (!oppdRad.NyPrisVeiledende.HasValue)
                {
                    oppdRad.NyPrisVeiledende = funnet.PrisVeiledende;
                }
            }

            resultat.Add(oppdRad);
        }

        return resultat;
    }

    public async Task<(int OppdatertNaa, int Planlagt)> OppdaterAsync(List<OppdateringRad> rader, DateTime gjelderFraDato, string leverandor)
    {
        var oppdatertNaa = 0;
        var planlagt = 0;
        var idag = DateTime.Today;

        foreach (var rad in rader.Where(r => r.Inkluder && r.Feil is null && r.ErEndret && r.ComponentId.HasValue))
        {
            if (gjelderFraDato.Date <= idag)
            {
                var comp = await db.Components.FindAsync(rad.ComponentId!.Value);
                if (comp is null)
                {
                    continue;
                }

                PrisHistorikkLogger.Logg(db, comp, rad.NyPrisNetto, rad.NyPrisVeiledende, $"Prisoppdatering ({leverandor})");
                comp.PrisNetto = rad.NyPrisNetto;
                comp.PrisVeiledende = rad.NyPrisVeiledende;
                oppdatertNaa++;
            }
            else
            {
                db.PlanlagtePrisendringer.Add(new PlanlagtPrisendring
                {
                    ComponentId = rad.ComponentId!.Value,
                    GammelPrisNetto = rad.GammelPrisNetto,
                    GammelPrisVeiledende = rad.GammelPrisVeiledende,
                    NyPrisNetto = rad.NyPrisNetto,
                    NyPrisVeiledende = rad.NyPrisVeiledende,
                    GjelderFraDato = gjelderFraDato.Date,
                    Kilde = leverandor
                });
                planlagt++;
            }
        }

        await db.SaveChangesAsync();
        return (oppdatertNaa, planlagt);
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
