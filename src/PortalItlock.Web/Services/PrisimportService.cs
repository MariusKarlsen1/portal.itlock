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
        public string? Produsent { get; set; }
        public string? ProdusentAdresse { get; set; }
        public string? ProdusentPostnr { get; set; }
        public string? ProdusentSted { get; set; }
        public string? ProdusentLand { get; set; }
        public string? ProdusentOrgnr { get; set; }
        public string? Beskrivelse { get; set; }
        public string? Varegruppe { get; set; }
        public string? Overflate { get; set; }
        public string? Enhet { get; set; }
        public decimal? PrisNetto { get; set; }
        public decimal? PrisVeiledende { get; set; }
        public int? MontasjeMinutterProsjekt { get; set; }
        public int? MontasjeMinutterArbeidsordre { get; set; }
        public int? MontasjeMinutterService { get; set; }
        public bool? LagervareVerdi { get; set; }
        public bool? AktivVerdi { get; set; }
        public bool ErNyVare { get; set; }
        public int? EksisterendeComponentId { get; set; }
        public string? EksisterendeNavn { get; set; }
        public string? RabattgruppeKode { get; set; }
        public int? RabattgruppeId { get; set; }
        public string? KomponenttypeNavn { get; set; }
        public int? ComponentTypeId { get; set; }
        public string? ProduktgruppeNavn { get; set; }
        public int? ProduktgruppeId { get; set; }
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
        public int? Produsent { get; set; }
        public int? ProdusentAdresse { get; set; }
        public int? ProdusentPostnr { get; set; }
        public int? ProdusentSted { get; set; }
        public int? ProdusentLand { get; set; }
        public int? ProdusentOrgnr { get; set; }
        public int? Beskrivelse { get; set; }
        public int? Varegruppe { get; set; }
        public int? Overflate { get; set; }
        public int? Enhet { get; set; }
        public int? PrisNetto { get; set; }
        public int? PrisVeiledende { get; set; }
        public int? MontasjeMinutterProsjekt { get; set; }
        public int? MontasjeMinutterArbeidsordre { get; set; }
        public int? MontasjeMinutterService { get; set; }
        public int? Lager { get; set; }
        public int? Inaktiv { get; set; }
        public int? Rabattgruppe { get; set; }
        public int? Komponenttype { get; set; }
        public int? Produktgruppe { get; set; }
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
            Produsent = Finn("produsent", "manufacturer", "brand", "merke"),
            ProdusentAdresse = Finn("produsentadresse", "produsent adresse"),
            ProdusentPostnr = Finn("produsentpostnr", "produsent postnr"),
            ProdusentSted = Finn("produsentsted", "produsent sted"),
            ProdusentLand = Finn("produsentland", "produsent land"),
            ProdusentOrgnr = Finn("produsentorgnr", "produsent org"),
            Beskrivelse = Finn("beskrivelse", "varebeskrivelse", "produktbeskrivelse"),
            Varegruppe = Finn("varegruppe", "kategori"),
            Overflate = Finn("overflate", "finish"),
            Enhet = Finn("enhet", "måleenhet", "maaleenhet", "unit"),
            PrisNetto = Finn("nettopris", "netto pris", "innkjøpspris", "innkjøp", "kostpris", "netto"),
            PrisVeiledende = Finn("veiledende", "utpris", "listepris", "bruttopris", "veil.", "veil pris", "salgspris"),
            MontasjeMinutterProsjekt = Finn("montasjetid prosjekt", "montasje prosjekt"),
            MontasjeMinutterArbeidsordre = Finn("montasjetid arbeidsordre", "montasje arbeidsordre"),
            MontasjeMinutterService = Finn("montasjetid service", "montasje service"),
            Lager = Finn("lager", "lagervare"),
            Inaktiv = Finn("inaktiv"),
            Rabattgruppe = Finn("rabattgruppe", "rabatt gruppe", "rabattkode"),
            Komponenttype = Finn("komponenttype", "komponent type"),
            Produktgruppe = Finn("produktgruppe", "produkt gruppe"),
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

        var rabattgrupperPerKode = (await db.Rabattgrupper
                .Where(r => r.Leverandor.ToLower() == leverandor.ToLower() && r.Aktiv)
                .ToListAsync())
            .GroupBy(r => r.Kode.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var komponenttyperPerNavn = (await db.ComponentTypes.ToListAsync())
            .GroupBy(t => t.Navn.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var produktgrupperPerNavn = (await db.Produktgrupper.ToListAsync())
            .GroupBy(p => p.Navn.Trim().ToLowerInvariant())
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
            bool? lagervareVerdi = lagerVerdi?.ToUpperInvariant() switch
            {
                "J" => true,
                "N" => false,
                _ => null,
            };

            var aktivVerdi = HentFelt(rad, valgt.Inaktiv)?.Trim();
            bool? aktivFlagg = aktivVerdi?.ToUpperInvariant() switch
            {
                "A" => true,
                "I" => false,
                _ => null,
            };

            var rabattgruppeVerdi = HentFelt(rad, valgt.Rabattgruppe)?.Trim();
            Rabattgruppe? importertRabattgruppe = null;
            if (!string.IsNullOrEmpty(rabattgruppeVerdi))
            {
                rabattgrupperPerKode.TryGetValue(rabattgruppeVerdi.ToLowerInvariant(), out importertRabattgruppe);
            }

            var komponenttypeVerdi = HentFelt(rad, valgt.Komponenttype)?.Trim();
            ComponentType? importertKomponenttype = null;
            if (!string.IsNullOrEmpty(komponenttypeVerdi))
            {
                komponenttyperPerNavn.TryGetValue(komponenttypeVerdi.ToLowerInvariant(), out importertKomponenttype);
            }

            var produktgruppeVerdi = HentFelt(rad, valgt.Produktgruppe)?.Trim();
            Produktgruppe? importertProduktgruppe = null;
            if (!string.IsNullOrEmpty(produktgruppeVerdi))
            {
                produktgrupperPerNavn.TryGetValue(produktgruppeVerdi.ToLowerInvariant(), out importertProduktgruppe);
            }

            var importRad = new ImportRad
            {
                RadNummer = i + 2,
                Produktkode = produktkode,
                Navn = navn,
                Navn2 = HentFelt(rad, valgt.Navn2),
                Produsent = HentFelt(rad, valgt.Produsent),
                ProdusentAdresse = HentFelt(rad, valgt.ProdusentAdresse),
                ProdusentPostnr = HentFelt(rad, valgt.ProdusentPostnr),
                ProdusentSted = HentFelt(rad, valgt.ProdusentSted),
                ProdusentLand = HentFelt(rad, valgt.ProdusentLand),
                ProdusentOrgnr = HentFelt(rad, valgt.ProdusentOrgnr),
                Beskrivelse = HentFelt(rad, valgt.Beskrivelse),
                Varegruppe = HentFelt(rad, valgt.Varegruppe),
                Overflate = HentFelt(rad, valgt.Overflate),
                Enhet = NormaliserEnhet(HentFelt(rad, valgt.Enhet)),
                PrisNetto = ParsePris(HentFelt(rad, valgt.PrisNetto)),
                PrisVeiledende = ParsePris(HentFelt(rad, valgt.PrisVeiledende)),
                MontasjeMinutterProsjekt = ParseHeltall(HentFelt(rad, valgt.MontasjeMinutterProsjekt)),
                MontasjeMinutterArbeidsordre = ParseHeltall(HentFelt(rad, valgt.MontasjeMinutterArbeidsordre)),
                MontasjeMinutterService = ParseHeltall(HentFelt(rad, valgt.MontasjeMinutterService)),
                LagervareVerdi = lagervareVerdi,
                AktivVerdi = aktivFlagg,
                KomponenttypeNavn = importertKomponenttype?.Navn,
                ComponentTypeId = importertKomponenttype?.Id,
                ProduktgruppeNavn = importertProduktgruppe?.Navn,
                ProduktgruppeId = importertProduktgruppe?.Id,
            };

            void SettRabattgruppe(Rabattgruppe gruppe)
            {
                importRad.RabattgruppeId = gruppe.Id;
                importRad.RabattgruppeKode = gruppe.Kode;
                if (importRad.PrisVeiledende.HasValue)
                {
                    importRad.PrisNetto = Math.Round(importRad.PrisVeiledende.Value * (1 - gruppe.RabattProsent / 100m), 2);
                    importRad.NettoErBeregnet = true;
                }
            }

            if (string.IsNullOrWhiteSpace(produktkode))
            {
                importRad.Feil = "Mangler produktkode";
            }
            else if (eksisterendePerKode.TryGetValue(produktkode.Trim().ToLowerInvariant(), out var funnet))
            {
                importRad.EksisterendeComponentId = funnet.Id;
                importRad.EksisterendeNavn = funnet.Navn;
                importRad.ErNyVare = false;

                if (importertRabattgruppe is not null)
                {
                    SettRabattgruppe(importertRabattgruppe);
                }
                else if (funnet.Rabattgruppe is not null && importRad.PrisVeiledende.HasValue)
                {
                    importRad.RabattgruppeKode = funnet.Rabattgruppe.Kode;
                    importRad.PrisNetto = Math.Round(importRad.PrisVeiledende.Value * (1 - funnet.Rabattgruppe.RabattProsent / 100m), 2);
                    importRad.NettoErBeregnet = true;
                }
            }
            else
            {
                importRad.ErNyVare = true;
                if (importertRabattgruppe is not null)
                {
                    SettRabattgruppe(importertRabattgruppe);
                }
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
                if (!string.IsNullOrWhiteSpace(rad.Produsent))
                {
                    comp.Produsent = rad.Produsent;
                }
                if (!string.IsNullOrWhiteSpace(rad.ProdusentAdresse))
                {
                    comp.ProdusentAdresse = rad.ProdusentAdresse;
                }
                if (!string.IsNullOrWhiteSpace(rad.ProdusentPostnr))
                {
                    comp.ProdusentPostnr = rad.ProdusentPostnr;
                }
                if (!string.IsNullOrWhiteSpace(rad.ProdusentSted))
                {
                    comp.ProdusentSted = rad.ProdusentSted;
                }
                if (!string.IsNullOrWhiteSpace(rad.ProdusentLand))
                {
                    comp.ProdusentLand = rad.ProdusentLand;
                }
                if (!string.IsNullOrWhiteSpace(rad.ProdusentOrgnr))
                {
                    comp.ProdusentOrgnr = rad.ProdusentOrgnr;
                }
                if (!string.IsNullOrWhiteSpace(rad.Beskrivelse))
                {
                    comp.Beskrivelse = rad.Beskrivelse;
                }
                if (!string.IsNullOrWhiteSpace(rad.Varegruppe))
                {
                    comp.Varegruppe = rad.Varegruppe;
                }
                if (!string.IsNullOrWhiteSpace(rad.Overflate))
                {
                    comp.Overflate = rad.Overflate;
                }
                if (!string.IsNullOrWhiteSpace(rad.Enhet))
                {
                    comp.Enhet = rad.Enhet;
                }
                if (rad.MontasjeMinutterProsjekt.HasValue)
                {
                    comp.MontasjeMinutterProsjekt = rad.MontasjeMinutterProsjekt;
                }
                if (rad.MontasjeMinutterArbeidsordre.HasValue)
                {
                    comp.MontasjeMinutterArbeidsordre = rad.MontasjeMinutterArbeidsordre;
                }
                if (rad.MontasjeMinutterService.HasValue)
                {
                    comp.MontasjeMinutterService = rad.MontasjeMinutterService;
                }
                if (rad.LagervareVerdi.HasValue)
                {
                    comp.ILagerstyring = rad.LagervareVerdi.Value;
                }
                if (rad.AktivVerdi.HasValue)
                {
                    comp.Aktiv = rad.AktivVerdi.Value;
                }
                if (rad.RabattgruppeId.HasValue)
                {
                    comp.RabattgruppeId = rad.RabattgruppeId;
                }
                if (rad.ComponentTypeId.HasValue)
                {
                    comp.ComponentTypeId = rad.ComponentTypeId;
                }
                if (rad.ProduktgruppeId.HasValue)
                {
                    comp.ProduktgruppeId = rad.ProduktgruppeId;
                }

                oppdatert++;
            }
            else
            {
                db.Components.Add(new Component
                {
                    Navn = rad.Navn!,
                    Navn2 = string.IsNullOrWhiteSpace(rad.Navn2) ? null : rad.Navn2,
                    Produsent = string.IsNullOrWhiteSpace(rad.Produsent) ? null : rad.Produsent,
                    ProdusentAdresse = string.IsNullOrWhiteSpace(rad.ProdusentAdresse) ? null : rad.ProdusentAdresse,
                    ProdusentPostnr = string.IsNullOrWhiteSpace(rad.ProdusentPostnr) ? null : rad.ProdusentPostnr,
                    ProdusentSted = string.IsNullOrWhiteSpace(rad.ProdusentSted) ? null : rad.ProdusentSted,
                    ProdusentLand = string.IsNullOrWhiteSpace(rad.ProdusentLand) ? null : rad.ProdusentLand,
                    ProdusentOrgnr = string.IsNullOrWhiteSpace(rad.ProdusentOrgnr) ? null : rad.ProdusentOrgnr,
                    Beskrivelse = string.IsNullOrWhiteSpace(rad.Beskrivelse) ? null : rad.Beskrivelse,
                    Varegruppe = string.IsNullOrWhiteSpace(rad.Varegruppe) ? null : rad.Varegruppe,
                    Overflate = string.IsNullOrWhiteSpace(rad.Overflate) ? null : rad.Overflate,
                    Produktkode = rad.Produktkode,
                    Leverandor = leverandor,
                    Enhet = string.IsNullOrWhiteSpace(rad.Enhet) ? null : rad.Enhet,
                    PrisNetto = rad.PrisNetto,
                    PrisVeiledende = rad.PrisVeiledende,
                    MontasjeMinutterProsjekt = rad.MontasjeMinutterProsjekt,
                    MontasjeMinutterArbeidsordre = rad.MontasjeMinutterArbeidsordre,
                    MontasjeMinutterService = rad.MontasjeMinutterService,
                    ILagerstyring = rad.LagervareVerdi ?? false,
                    Aktiv = rad.AktivVerdi ?? true,
                    RabattgruppeId = rad.RabattgruppeId,
                    ComponentTypeId = rad.ComponentTypeId,
                    ProduktgruppeId = rad.ProduktgruppeId
                });
                nye++;
            }
        }

        await db.SaveChangesAsync();
        return (oppdatert, nye);
    }

    private static string? HentFelt(string[] rad, int? indeks) =>
        indeks.HasValue && indeks.Value >= 0 && indeks.Value < rad.Length ? rad[indeks.Value] : null;

    // Enkelte leverandører bruker tallkoder for enhet i prislistene sine i stedet for tekst.
    private static string? NormaliserEnhet(string? tekst)
    {
        var trimmet = tekst?.Trim();
        return trimmet switch
        {
            "1" => "Stk",
            "2" => "Sett",
            "3" => "m",
            _ => tekst,
        };
    }

    private static int? ParseHeltall(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return null;
        }

        return int.TryParse(tekst.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var verdi) ? verdi : null;
    }

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
