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
        public string? Gtin { get; set; }
        public string? Navn { get; set; }
        public string? Navn2 { get; set; }
        public string? Beskrivelse { get; set; }
        public string? Varegruppe { get; set; }
        public string? Overflate { get; set; }
        public string? Konsept { get; set; }
        public string? Enhet { get; set; }
        public decimal? PrisNetto { get; set; }
        public decimal? PrisVeiledende { get; set; }
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
        public int? Beskrivelse { get; set; }
        public int? Varegruppe { get; set; }
        public int? Overflate { get; set; }
        public int? Konsept { get; set; }
        public int? Enhet { get; set; }
        public int? PrisNetto { get; set; }
        public int? PrisVeiledende { get; set; }
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
            Beskrivelse = Finn("beskrivelse", "varebeskrivelse", "produktbeskrivelse"),
            Varegruppe = Finn("varegruppe", "kategori"),
            Overflate = Finn("overflate", "finish"),
            Konsept = Finn("konsept", "concept", "serie"),
            Enhet = Finn("enhet", "måleenhet", "maaleenhet", "unit"),
            PrisNetto = Finn("nettopris", "netto pris", "innkjøpspris", "innkjøp", "kostpris", "netto"),
            PrisVeiledende = Finn("veiledende", "utpris", "listepris", "bruttopris", "veil.", "veil pris", "salgspris"),
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
                Gtin = HentFelt(rad, valgt.Gtin),
                Navn = navn,
                Navn2 = HentFelt(rad, valgt.Navn2),
                Beskrivelse = HentFelt(rad, valgt.Beskrivelse),
                Varegruppe = HentFelt(rad, valgt.Varegruppe),
                Overflate = HentFelt(rad, valgt.Overflate),
                Konsept = HentFelt(rad, valgt.Konsept),
                Enhet = NormaliserEnhet(HentFelt(rad, valgt.Enhet)),
                PrisNetto = ParsePris(HentFelt(rad, valgt.PrisNetto)),
                PrisVeiledende = ParsePris(HentFelt(rad, valgt.PrisVeiledende)),
                LagervareVerdi = lagervareVerdi,
                AktivVerdi = aktivFlagg,
                KomponenttypeNavn = importertKomponenttype?.Navn,
                ComponentTypeId = importertKomponenttype?.Id,
                // Produktgruppe opprettes automatisk i ImporterAsync hvis den
                // ikke finnes fra før (samme mønster som leverandør) - viser
                // derfor navnet her selv når det ikke matchet noen eksisterende.
                ProduktgruppeNavn = importertProduktgruppe?.Navn ?? produktgruppeVerdi,
                ProduktgruppeId = importertProduktgruppe?.Id,
            };

            Component? funnet = null;
            if (string.IsNullOrWhiteSpace(produktkode))
            {
                importRad.Feil = "Mangler produktkode";
            }
            else if (eksisterendePerKode.TryGetValue(produktkode.Trim().ToLowerInvariant(), out funnet))
            {
                importRad.EksisterendeComponentId = funnet.Id;
                importRad.EksisterendeNavn = funnet.Navn;
                importRad.ErNyVare = false;
            }
            else
            {
                importRad.ErNyVare = true;
                if (string.IsNullOrWhiteSpace(navn))
                {
                    importRad.Feil = "Ny vare mangler navn";
                }
            }

            // Nettoprisen skal ALLTID beregnes som veiledende pris minus
            // rabattgruppens prosent når begge er kjent - uansett om filen
            // selv har en egen Pris netto-kolonne mappet eller ikke, og
            // uansett om rabattgruppen/veiledende kommer fra filen eller fra
            // varens eksisterende verdier (brukes som fallback når filen ikke
            // har kolonnen mappet, eller feltet står tomt på denne raden).
            // Filens egen Pris netto-kolonne brukes kun når ingen
            // rabattgruppe i det hele tatt er kjent for varen.
            var effektivRabattgruppe = importertRabattgruppe ?? funnet?.Rabattgruppe;
            if (effektivRabattgruppe is not null)
            {
                importRad.RabattgruppeId = effektivRabattgruppe.Id;
                importRad.RabattgruppeKode = effektivRabattgruppe.Kode;

                var effektivVeiledende = importRad.PrisVeiledende ?? funnet?.PrisVeiledende;
                if (effektivVeiledende.HasValue)
                {
                    importRad.PrisNetto = Math.Round(effektivVeiledende.Value * (1 - effektivRabattgruppe.RabattProsent / 100m), 2);
                    importRad.NettoErBeregnet = true;
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

        // Én leverandør-rad pr. import - slår opp/oppretter den én gang, og
        // kobler hver vare i denne prislisten mot den (se LeverandorSync).
        var leverandorEntitet = await LeverandorSync.FinnEllerOpprettAsync(db, leverandor);

        // Cacher opprettede produktgrupper pr. navn innenfor dette
        // importkjøret, slik at samme nye gruppenavn ikke opprettes flere
        // ganger (og slipper å lagre til DB for hver rad bare for å få en ID).
        var nyeProduktgrupperPerNavn = new Dictionary<string, Produktgruppe>(StringComparer.OrdinalIgnoreCase);

        var radeneSomSkalKjores = rader.Where(r => r.Inkluder && r.Feil is null).ToList();

        // Henter ALT vi trenger for eksisterende varer i noen få samlekall
        // FØR selve løkken, i stedet for separate databasekall pr. rad -
        // en fil på flere tusen rader (som denne) gjorde ellers titusenvis
        // av sekvensielle kall, trege nok til at Blazor-kretsen mot Railway
        // ble avbrutt midt i importen ("An unhandled error has occurred").
        //
        // Matcher FERSKT mot produktkode her (samme mønster som
        // ForhandsvisAsync), i stedet for å stole blindt på
        // rad.EksisterendeComponentId/ErNyVare fra forhåndsvisningen -
        // dén ble beregnet FØR denne kjøringen, og hvis brukeren prøver
        // igjen etter en delvis mislykket import (feks. et avbrutt forsøk
        // som rakk å lagre noen bolker), vet forhåndsvisningen ikke om
        // varer som allerede ble satt inn da. Uten fersk matching ville de
        // blitt satt inn PÅ NYTT som duplikater ved hvert nytt forsøk.
        var eksisterendePerKode = (await db.Components
                .Include(c => c.Produktgrupper)
                .Where(c => c.Leverandor != null && c.Leverandor.ToLower() == leverandor.ToLower() && c.Produktkode != null)
                .ToListAsync())
            .GroupBy(c => c.Produktkode!.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var eksisterendeIder = eksisterendePerKode.Values.Select(c => c.Id).ToList();

        // Henter i bolker på 500 ID-er av gangen - en fil på flere tusen
        // rader kan ellers lage en IN-klausul med flere tusen parametere i
        // ett kall, som risikerer å treffe SQLite sin grense for antall
        // parametere pr. spørring.
        var eksisterendeLenker = new List<ComponentLeverandor>();
        foreach (var idBolk in eksisterendeIder.Chunk(500))
        {
            eksisterendeLenker.AddRange(await db.ComponentLeverandorer
                .Where(cl => idBolk.Contains(cl.ComponentId))
                .ToListAsync());
        }

        var lenkerPerKomponentOgLeverandor = eksisterendeLenker
            .ToDictionary(cl => (cl.ComponentId, cl.LeverandorId));
        var komponenterMedMinstEnLenke = eksisterendeLenker
            .Select(cl => cl.ComponentId)
            .ToHashSet();

        var alleProduktgrupperPerId = await db.Produktgrupper.ToDictionaryAsync(p => p.Id);

        var behandlet = 0;
        foreach (var rad in radeneSomSkalKjores)
        {
            // Produktgruppen kan ha blitt matchet mot en eksisterende gruppe i
            // ForhandsvisAsync (ProduktgruppeId satt), eller kun mot et navn
            // som ikke fantes fra før - oppretter den i så fall her, samme
            // mønster som leverandøren over.
            if (!rad.ProduktgruppeId.HasValue && !string.IsNullOrWhiteSpace(rad.ProduktgruppeNavn))
            {
                if (!nyeProduktgrupperPerNavn.TryGetValue(rad.ProduktgruppeNavn, out var produktgruppe))
                {
                    produktgruppe = new Produktgruppe { Navn = rad.ProduktgruppeNavn.Trim() };
                    db.Produktgrupper.Add(produktgruppe);
                    await db.SaveChangesAsync();
                    nyeProduktgrupperPerNavn[rad.ProduktgruppeNavn] = produktgruppe;
                    alleProduktgrupperPerId[produktgruppe.Id] = produktgruppe;
                }
                rad.ProduktgruppeId = produktgruppe.Id;
            }

            var kode = rad.Produktkode!.Trim().ToLowerInvariant();
            if (eksisterendePerKode.TryGetValue(kode, out var comp))
            {
                var nyNetto = rad.PrisNetto ?? comp.PrisNetto;
                var nyVeil = rad.PrisVeiledende ?? comp.PrisVeiledende;
                PrisHistorikkLogger.Logg(db, comp, nyNetto, nyVeil, $"Prisimport ({leverandor})");

                comp.PrisNetto = nyNetto;
                comp.PrisVeiledende = nyVeil;
                if (!string.IsNullOrWhiteSpace(rad.Navn))
                {
                    comp.Navn = rad.Navn.Trim();
                }

                // Oppdaterer (eller oppretter, om koblingen mangler) denne
                // leverandørens egen varenummer/pris-kobling for varen -
                // uavhengig av om denne leverandøren er satt som standard.
                if (!lenkerPerKomponentOgLeverandor.TryGetValue((comp.Id, leverandorEntitet.Id), out var lenke))
                {
                    var harAndreLenker = komponenterMedMinstEnLenke.Contains(comp.Id);
                    lenke = new ComponentLeverandor
                    {
                        ComponentId = comp.Id,
                        LeverandorId = leverandorEntitet.Id,
                        ErStandard = !harAndreLenker
                    };
                    db.ComponentLeverandorer.Add(lenke);
                    lenkerPerKomponentOgLeverandor[(comp.Id, leverandorEntitet.Id)] = lenke;
                    komponenterMedMinstEnLenke.Add(comp.Id);
                }
                lenke.Varenummer = rad.Produktkode;
                lenke.PrisNetto = nyNetto;
                lenke.PrisVeiledende = nyVeil;
                if (string.IsNullOrWhiteSpace(lenke.Navn))
                {
                    lenke.Navn = comp.Navn;
                }
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
                if (!string.IsNullOrWhiteSpace(rad.Overflate))
                {
                    comp.Overflate = rad.Overflate;
                }
                if (!string.IsNullOrWhiteSpace(rad.Konsept))
                {
                    comp.Konsept = rad.Konsept.Trim();
                }
                if (!string.IsNullOrWhiteSpace(rad.Gtin))
                {
                    comp.Gtin = rad.Gtin.Trim();
                }
                if (!string.IsNullOrWhiteSpace(rad.Enhet))
                {
                    comp.Enhet = rad.Enhet;
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
                if (rad.ProduktgruppeId.HasValue && comp.Produktgrupper.All(p => p.Id != rad.ProduktgruppeId.Value)
                    && alleProduktgrupperPerId.TryGetValue(rad.ProduktgruppeId.Value, out var eksisterendeGruppe))
                {
                    comp.Produktgrupper.Add(eksisterendeGruppe);
                }

                oppdatert++;
            }
            else
            {
                var nyKomponent = new Component
                {
                    Navn = rad.Navn!,
                    Navn2 = string.IsNullOrWhiteSpace(rad.Navn2) ? null : rad.Navn2,
                    Beskrivelse = string.IsNullOrWhiteSpace(rad.Beskrivelse) ? null : rad.Beskrivelse,
                    Varegruppe = string.IsNullOrWhiteSpace(rad.Varegruppe) ? null : rad.Varegruppe,
                    Overflate = string.IsNullOrWhiteSpace(rad.Overflate) ? null : rad.Overflate,
                    Konsept = string.IsNullOrWhiteSpace(rad.Konsept) ? null : rad.Konsept.Trim(),
                    Produktkode = rad.Produktkode,
                    Gtin = string.IsNullOrWhiteSpace(rad.Gtin) ? null : rad.Gtin.Trim(),
                    Leverandor = leverandor,
                    Enhet = string.IsNullOrWhiteSpace(rad.Enhet) ? null : rad.Enhet,
                    PrisNetto = rad.PrisNetto,
                    PrisVeiledende = rad.PrisVeiledende,
                    ILagerstyring = rad.LagervareVerdi ?? false,
                    Aktiv = rad.AktivVerdi ?? true,
                    RabattgruppeId = rad.RabattgruppeId,
                    ComponentTypeId = rad.ComponentTypeId
                };
                // Nye varer er alltid knyttet til seg selv (leverandøren de
                // ble importert fra) som standard - se LeverandorSync.
                nyKomponent.Leverandorer.Add(new ComponentLeverandor
                {
                    LeverandorId = leverandorEntitet.Id,
                    Varenummer = rad.Produktkode,
                    Navn = nyKomponent.Navn,
                    PrisNetto = rad.PrisNetto,
                    PrisVeiledende = rad.PrisVeiledende,
                    ErStandard = true
                });
                db.Components.Add(nyKomponent);

                if (rad.ProduktgruppeId.HasValue && alleProduktgrupperPerId.TryGetValue(rad.ProduktgruppeId.Value, out var nyGruppe))
                {
                    nyKomponent.Produktgrupper.Add(nyGruppe);
                }

                // Registreres med en gang, slik at en senere rad i SAMME fil
                // med samme produktkode (eller et nytt forsøk etter en
                // avbrutt import) oppdaterer denne i stedet for å opprette
                // enda et duplikat.
                eksisterendePerKode[kode] = nyKomponent;

                nye++;
            }

            // Lagrer i bolker i stedet for å holde HELE importen som én
            // kjempetransaksjon - en fil på flere tusen rader ga ellers én
            // enkelt SaveChangesAsync med tusenvis av endrede entiteter helt
            // til slutt, noe som tok så lang tid at Railway-forbindelsen ble
            // brutt midt i (se kommentaren ved preloading over).
            behandlet++;
            if (behandlet % 250 == 0)
            {
                await db.SaveChangesAsync();
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
            "2" => "Par",
            "3" => "M",
            _ => tekst,
        };
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
