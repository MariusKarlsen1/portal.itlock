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
        public string? Navn2 { get; set; }
        public string? Beskrivelse { get; set; }
        public string? Varegruppe { get; set; }
        public string? Enhet { get; set; }
        public decimal? PrisNetto { get; set; }
        public decimal? PrisVeiledende { get; set; }
        public bool SettLagervare { get; set; }
        public bool SettInaktiv { get; set; }
        public bool ErNyVare { get; set; }
        public int? EksisterendeComponentId { get; set; }
        public string? EksisterendeNavn { get; set; }
        public string? RabattgruppeKode { get; set; }
        public bool NettoErBeregnet { get; set; }
        public string? Feil { get; set; }
        public bool Inkluder { get; set; } = true;
    }

    // Brukes baade som "hva vi automatisk foreslo" og som brukerens gjeldende
    // kolonnevalg i mappingskjemaet - samme form paa begge, saa dette er den ene klassen.
    public sealed class KolonneForslag
    {
        public int? Produktkode { get; set; }
        public int? Gtin { get; set; }
        public int? Navn { get; set; }
        public int? Navn2 { get; set; }
        public int? Beskrivelse { get; set; }
        public int? Varegruppe { get; set; }
        public int? Enhet { get; set; }
        public int? PrisNetto { get; set; }
        public int? PrisVeiledende { get; set; }
        public int? Lager { get; set; }
        public int? Inaktiv { get; set; }
    }

    // Finner sannsynlig kolonne per felt ut fra overskriftstekst, slik at brukeren
    // sjelden trenger aa mappe kolonner manuelt - kun rette opp der gjettingen bommer.
    public static KolonneForslag AutoMatchKolonner(List<ArkKolonne> kolonner)
    {
        int? Finn(params string[] nokkelord) => kolonner
            .FirstOrDefault(k => nokkelord.Any(n => k.Overskrift.Contains(n, StringComparison.OrdinalIgnoreCase)))
            ?.Indeks;

        return new KolonneForslag
        {
            Produktkode = Finn("produktkode", "artikkelnr", "varenr", "art.nr", "art nr", "sku", "vare nr", "produktnr"),
            Gtin = Finn("gtin", "ean", "strekkode", "barcode"),
            Navn = Finn("varenavn 1", "varenavn1", "navn", "produktnavn", "varetekst"),
            Navn2 = Finn("varenavn 2", "varenavn2", "navn 2"),
            Beskrivelse = Finn("beskrivelse", "varebeskrivelse", "produktbeskrivelse"),
            Varegruppe = Finn("varegruppe", "produktgruppe", "kategori"),
            Enhet = Finn("enhet", "måleenhet", "maaleenhet", "unit"),
            PrisNetto = Finn("nettopris", "netto pris", "innkjøpspris", "innkjøp", "kostpris", "netto"),
            PrisVeiledende = Finn("veiledende", "utpris", "listepris", "bruttopris", "veil.", "veil pris", "salgspris"),
            Lager = Finn("lager", "lagervare"),
            Inaktiv = Finn("inaktiv"),
        };
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

    public async Task<List<ImportRad>> ForhandsvisAsync(List<string[]> rader, string leverandor, KolonneForslag valgt)
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
            var produktkode = HentFelt(rad, valgt.Produktkode);
            var navn = HentFelt(rad, valgt.Navn);

            if (string.IsNullOrWhiteSpace(produktkode) && string.IsNullOrWhiteSpace(navn))
            {
                continue;
            }

            var lagerVerdi = HentFelt(rad, valgt.Lager)?.Trim();
            var inaktivVerdi = HentFelt(rad, valgt.Inaktiv)?.Trim();

            var importRad = new ImportRad
            {
                RadNummer = i + 2,
                Produktkode = produktkode,
                Navn = navn,
                Navn2 = HentFelt(rad, valgt.Navn2),
                Beskrivelse = HentFelt(rad, valgt.Beskrivelse),
                Varegruppe = HentFelt(rad, valgt.Varegruppe),
                Enhet = HentFelt(rad, valgt.Enhet),
                PrisNetto = ParsePris(HentFelt(rad, valgt.PrisNetto)),
                PrisVeiledende = ParsePris(HentFelt(rad, valgt.PrisVeiledende)),
                SettLagervare = !string.IsNullOrEmpty(lagerVerdi) && lagerVerdi.Equals("J", StringComparison.OrdinalIgnoreCase),
                SettInaktiv = !string.IsNullOrEmpty(inaktivVerdi) && inaktivVerdi.Equals("I", StringComparison.OrdinalIgnoreCase),
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
                if (!string.IsNullOrWhiteSpace(rad.Navn2))
                {
                    comp.Navn2 = rad.Navn2;
                }
                if (!string.IsNullOrWhiteSpace(rad.Beskrivelse))
                {
                    comp.Beskrivelse = rad.Beskrivelse;
                }
                if (!string.IsNullOrWhiteSpace(rad.Varegruppe))
                {
                    comp.Varegruppe = rad.Varegruppe;
                }
                if (rad.SettLagervare)
                {
                    comp.ILagerstyring = true;
                }
                if (rad.SettInaktiv)
                {
                    comp.Aktiv = false;
                }

                oppdatert++;
            }
            else
            {
                db.Components.Add(new Component
                {
                    Navn = rad.Navn!,
                    Navn2 = string.IsNullOrWhiteSpace(rad.Navn2) ? null : rad.Navn2,
                    Beskrivelse = string.IsNullOrWhiteSpace(rad.Beskrivelse) ? null : rad.Beskrivelse,
                    Varegruppe = string.IsNullOrWhiteSpace(rad.Varegruppe) ? null : rad.Varegruppe,
                    Produktkode = rad.Produktkode,
                    Leverandor = leverandor,
                    Enhet = string.IsNullOrWhiteSpace(rad.Enhet) ? null : rad.Enhet,
                    PrisNetto = rad.PrisNetto,
                    PrisVeiledende = rad.PrisVeiledende,
                    ILagerstyring = rad.SettLagervare,
                    Aktiv = !rad.SettInaktiv
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
