using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Årlig prisoppdatering fra én leverandør: varenummeret i filen sjekkes mot
// leverandørens EGET varenummer på varen (ComponentLeverandor.Varenummer), med
// varens produktkode og GTIN/EAN som reserve. Bare prisene endres - aldri navn
// eller andre felt, og det lages ALDRI nye varer (det gjør Prisimport). Prisen
// kan settes til å gjelde fra en senere dato (PlanlagtPrisendring).
public class PrisoppdateringService(ApplicationDbContext db)
{
    public sealed class OppdateringRad
    {
        public int RadNummer { get; set; }
        public string? Varenummer { get; set; }
        public string? Gtin { get; set; }
        public decimal? NyPrisNetto { get; set; }
        public decimal? NyPrisVeiledende { get; set; }

        public int? ComponentId { get; set; }
        public string? ComponentNavn { get; set; }
        public decimal? GammelPrisNetto { get; set; }
        public decimal? GammelPrisVeiledende { get; set; }

        // Netto regnet ut fra ny veiledende og varens rabattgruppe (filen hadde ikke netto).
        public bool NettoErBeregnet { get; set; }

        public bool ErEndret { get; set; }
        public string? Feil { get; set; }
        public bool Inkluder { get; set; } = true;

        // Endring i netto (eller veiledende om netto mangler) i prosent - til kontrollen.
        public decimal? EndringProsent
        {
            get
            {
                if (GammelPrisNetto is > 0 && NyPrisNetto is { } nyN)
                {
                    return (nyN - GammelPrisNetto.Value) / GammelPrisNetto.Value * 100m;
                }

                return GammelPrisVeiledende is > 0 && NyPrisVeiledende is { } nyV
                    ? (nyV - GammelPrisVeiledende.Value) / GammelPrisVeiledende.Value * 100m
                    : null;
            }
        }
    }

    private sealed class Kobling
    {
        public int ComponentId { get; init; }
        public string Navn { get; init; } = "";
        public string? Varenummer { get; init; }
        public string? Produktkode { get; init; }
        public string? Gtin { get; init; }
        public decimal? LenkeNetto { get; init; }
        public decimal? LenkeVeiledende { get; init; }
        public decimal? VareNetto { get; init; }
        public decimal? VareVeiledende { get; init; }
        public decimal? RabattProsent { get; init; }
    }

    public async Task<List<OppdateringRad>> ForhandsvisAsync(
        List<string[]> rader, int leverandorId,
        int? varenummerKol, int? gtinKol, int? prisNettoKol, int? prisVeiledendeKol)
    {
        // Projiseres rett i SQL - unngår å laste hele Component (med filer) for
        // alle varene til leverandøren.
        var koblinger = await db.ComponentLeverandorer
            .Where(l => l.LeverandorId == leverandorId)
            .Select(l => new Kobling
            {
                ComponentId = l.ComponentId,
                Navn = l.Component!.Navn,
                Varenummer = l.Varenummer,
                Produktkode = l.Component.Produktkode,
                Gtin = l.Component.Gtin,
                LenkeNetto = l.PrisNetto,
                LenkeVeiledende = l.PrisVeiledende,
                VareNetto = l.Component.PrisNetto,
                VareVeiledende = l.Component.PrisVeiledende,
                RabattProsent = l.Component.Rabattgruppe != null && l.Component.Rabattgruppe.Aktiv ? l.Component.Rabattgruppe.RabattProsent : null
            })
            .ToListAsync();

        static string Nokkel(string s) => s.Trim().ToLowerInvariant();
        Dictionary<string, Kobling> Oppslag(Func<Kobling, string?> felt) => koblinger
            .Where(k => !string.IsNullOrWhiteSpace(felt(k)))
            .GroupBy(k => Nokkel(felt(k)!))
            .ToDictionary(g => g.Key, g => g.First());

        var perVarenummer = Oppslag(k => k.Varenummer);
        var perProduktkode = Oppslag(k => k.Produktkode);
        var perGtin = Oppslag(k => k.Gtin);

        var resultat = new List<OppdateringRad>();
        for (var i = 0; i < rader.Count; i++)
        {
            var rad = rader[i];
            var varenummer = HentFelt(rad, varenummerKol);
            var gtin = HentFelt(rad, gtinKol);

            if (string.IsNullOrWhiteSpace(varenummer) && string.IsNullOrWhiteSpace(gtin))
            {
                continue;
            }

            var oppdRad = new OppdateringRad
            {
                RadNummer = i + 2,
                Varenummer = varenummer,
                Gtin = gtin,
                NyPrisNetto = ParsePris(HentFelt(rad, prisNettoKol)),
                NyPrisVeiledende = ParsePris(HentFelt(rad, prisVeiledendeKol))
            };

            Kobling? funnet = null;
            if (!string.IsNullOrWhiteSpace(varenummer))
            {
                var n = Nokkel(varenummer);
                if (!perVarenummer.TryGetValue(n, out funnet))
                {
                    perProduktkode.TryGetValue(n, out funnet);
                }
            }

            if (funnet is null && !string.IsNullOrWhiteSpace(gtin))
            {
                perGtin.TryGetValue(Nokkel(gtin), out funnet);
            }

            if (funnet is null)
            {
                oppdRad.Feil = "Ikke funnet";
                oppdRad.Inkluder = false;
                resultat.Add(oppdRad);
                continue;
            }

            oppdRad.ComponentId = funnet.ComponentId;
            oppdRad.ComponentNavn = funnet.Navn;
            // Leverandørens egne priser på koblingen, ellers varens.
            oppdRad.GammelPrisNetto = funnet.LenkeNetto ?? funnet.VareNetto;
            oppdRad.GammelPrisVeiledende = funnet.LenkeVeiledende ?? funnet.VareVeiledende;

            // Har filen bare veiledende pris og varen en rabattgruppe, regnes netto ut.
            if (oppdRad.NyPrisNetto is null && oppdRad.NyPrisVeiledende is { } veil && funnet.RabattProsent is { } rabatt)
            {
                oppdRad.NyPrisNetto = Math.Round(veil * (1 - rabatt / 100m), 2);
                oppdRad.NettoErBeregnet = true;
            }

            oppdRad.NyPrisNetto ??= oppdRad.GammelPrisNetto;
            oppdRad.NyPrisVeiledende ??= oppdRad.GammelPrisVeiledende;
            oppdRad.ErEndret = oppdRad.NyPrisNetto != oppdRad.GammelPrisNetto || oppdRad.NyPrisVeiledende != oppdRad.GammelPrisVeiledende;
            oppdRad.Inkluder = oppdRad.ErEndret;
            resultat.Add(oppdRad);
        }

        return resultat;
    }

    public async Task<(int OppdatertNaa, int Planlagt)> OppdaterAsync(List<OppdateringRad> rader, DateTime gjelderFraDato, int leverandorId, string leverandorNavn)
    {
        var valgte = rader.Where(r => r.Inkluder && r.Feil is null && r.ErEndret && r.ComponentId.HasValue).ToList();
        if (valgte.Count == 0)
        {
            return (0, 0);
        }

        if (gjelderFraDato.Date > DateTime.Today)
        {
            foreach (var rad in valgte)
            {
                db.PlanlagtePrisendringer.Add(new PlanlagtPrisendring
                {
                    ComponentId = rad.ComponentId!.Value,
                    LeverandorId = leverandorId,
                    GammelPrisNetto = rad.GammelPrisNetto,
                    GammelPrisVeiledende = rad.GammelPrisVeiledende,
                    NyPrisNetto = rad.NyPrisNetto,
                    NyPrisVeiledende = rad.NyPrisVeiledende,
                    GjelderFraDato = gjelderFraDato.Date,
                    Kilde = leverandorNavn
                });
            }

            await db.SaveChangesAsync();
            return (0, valgte.Count);
        }

        var oppdatert = 0;
        foreach (var bit in valgte.Chunk(400))
        {
            var ider = bit.Select(r => r.ComponentId!.Value).ToList();
            var lenker = await db.ComponentLeverandorer
                .Where(l => l.LeverandorId == leverandorId && ider.Contains(l.ComponentId))
                .ToDictionaryAsync(l => l.ComponentId);
            var antallLenker = await db.ComponentLeverandorer
                .Where(l => ider.Contains(l.ComponentId))
                .GroupBy(l => l.ComponentId)
                .Select(g => new { g.Key, Antall = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Antall);
            var varer = await db.Components.Where(c => ider.Contains(c.Id)).ToDictionaryAsync(c => c.Id);

            foreach (var rad in bit)
            {
                if (!varer.TryGetValue(rad.ComponentId!.Value, out var comp))
                {
                    continue;
                }

                lenker.TryGetValue(comp.Id, out var lenke);
                SettPris(db, comp, lenke, antallLenker.GetValueOrDefault(comp.Id), rad.NyPrisNetto, rad.NyPrisVeiledende, $"Prisoppdatering ({leverandorNavn})");
                oppdatert++;
            }

            await db.SaveChangesAsync();
        }

        return (oppdatert, 0);
    }

    // Felles for prisoppdatering nå og planlagt prisendring: leverandørens pris
    // på koblingen oppdateres alltid, og varens egen pris når leverandøren er
    // standard for varen (eller varen ikke har noen annen leverandør).
    internal static void SettPris(ApplicationDbContext db, Component comp, ComponentLeverandor? lenke, int antallLenker, decimal? nyNetto, decimal? nyVeil, string kilde)
    {
        if (lenke is not null)
        {
            lenke.PrisNetto = nyNetto;
            lenke.PrisVeiledende = nyVeil;
        }

        if (lenke is null || lenke.ErStandard || antallLenker <= 1)
        {
            PrisHistorikkLogger.Logg(db, comp, nyNetto, nyVeil, kilde);
            comp.PrisNetto = nyNetto;
            comp.PrisVeiledende = nyVeil;
        }
    }

    private static string? HentFelt(string[] rad, int? indeks) =>
        indeks.HasValue && indeks.Value >= 0 && indeks.Value < rad.Length ? rad[indeks.Value] : null;

    private static decimal? ParsePris(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return null;
        }

        var rens = tekst.Replace("kr", "", StringComparison.OrdinalIgnoreCase).Replace(",-", "").Replace(" ", "").Replace(" ", "").Trim();
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
