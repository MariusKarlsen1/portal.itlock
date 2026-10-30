using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Kassa: salg, kreditnota, dagsoppgjør og bankavstemming. Se Models/Kassa.cs.
//
// Reglene som gjør at kontantfakturaene holder:
//  - Hvert salg og hver kreditnota får neste nummer i én felles serie
//    (unik indeks på Nummer), og raden endres aldri etterpå.
//  - Feil rettes med kreditnota som viser til salget den gjelder.
//  - Dagsoppgjøret tar alle salg som ikke er med i et oppgjør ennå, og låses.
public partial class KassaService(ApplicationDbContext db, LagerService lager, TilleggsmodulService tilleggsmoduler)
{
    public sealed record Linje(int? KasseVareId, string Navn, decimal Enhetspris, decimal MvaSats, int Antall, string? Kategori = null);

    public sealed record DagTall(int AntallSalg, int AntallKreditnota, decimal Omsetning, decimal Mva, decimal Kort, decimal Kontant,
        decimal KortTilbake, decimal KontantTilbake, int? FraNummer, int? TilNummer)
    {
        public decimal NettoKort => Kort + KortTilbake;
        public decimal NettoKontant => Kontant + KontantTilbake;
    }

    public sealed record Mvalinje(decimal Sats, decimal Grunnlag, decimal Mva);

    public static decimal Rund(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public static decimal MvaAv(decimal belopInkl, decimal sats) => sats <= 0 ? 0 : Rund(belopInkl - belopInkl / (1 + sats / 100m));

    // ---------- Varer ----------

    // Kassevare laget fra en vare i varekartoteket: veiledende pris (eks. mva)
    // pluss 25 % mva, varetypen som kategori og varens strekkode.
    public static KasseVare FraKomponent(Component c) => new()
    {
        Navn = c.Navn,
        Kategori = string.IsNullOrWhiteSpace(c.Type?.Navn) ? "Annet" : c.Type!.Navn,
        ComponentId = c.Id,
        PrisInklMva = Rund((c.PrisVeiledende ?? c.PrisNetto ?? 0) * 1.25m),
        Strekkode = string.IsNullOrWhiteSpace(c.Gtin) ? null : c.Gtin,
        Ikon = IkonFor(c.Type?.Navn)
    };

    // Symbol for en kategori når varen ikke har bilde (se KassaIkon).
    public static string IkonFor(string? kategori)
    {
        var k = (kategori ?? "").ToLowerInvariant();
        return k switch
        {
            _ when k.Contains("sylinder") => "sylinder",
            _ when k.Contains("nøkl") || k.Contains("nøkkel") || k.Contains("nokkel") => "nokkel",
            _ when k.Contains("hengelås") => "hengelas",
            _ when k.Contains("lås") || k.Contains("sluttstykke") => "las",
            _ when k.Contains("dør") || k.Contains("pumpe") || k.Contains("lukker") || k.Contains("vrider") || k.Contains("beslag") => "dor",
            _ when k.Contains("adgang") || k.Contains("leser") || k.Contains("kort") || k.Contains("sentral") || k.Contains("automatikk") => "brikke",
            _ when k.Contains("batteri") => "batteri",
            _ when k.Contains("verktøy") || k.Contains("skrue") => "verktoy",
            _ when k.Contains("skap") => "skap",
            _ => "boks"
        };
    }

    // Alle lagerførte varer ligger i kassa av seg selv: varer som er lagerført,
    // men ikke har en kassevare ennå, får en. Varer som er tatt ut av kassa
    // (Aktiv = false) legges ikke inn på nytt. Salg trekkes fra lageret.
    public async Task<int> SynkLagervarerAsync()
    {
        if (!await tilleggsmoduler.HarAsync(TilleggsModuler.Lager))
        {
            return 0;
        }

        var har = await db.KasseVarer.Where(v => v.ComponentId != null).Select(v => v.ComponentId!.Value).ToListAsync();
        var nye = await db.Components.Include(c => c.Type)
            .Where(c => c.ILagerstyring && c.Aktiv && !har.Contains(c.Id)).ToListAsync();
        if (nye.Count == 0)
        {
            return 0;
        }

        db.KasseVarer.AddRange(nye.Select(FraKomponent));
        await db.SaveChangesAsync();
        return nye.Count;
    }

    public async Task<KasseOppsett> OppsettAsync()
    {
        var o = await db.KasseOppsett.FirstOrDefaultAsync();
        if (o is null)
        {
            o = new KasseOppsett();
            db.KasseOppsett.Add(o);
            await db.SaveChangesAsync();
        }

        return o;
    }

    // ---------- Salg ----------

    public async Task<KasseSalg> SelgAsync(IReadOnlyList<Linje> linjer, KasseSalgType type, decimal rabattProsent, decimal? mottatt,
        string? kundeNavn, string? kundeEpost, int? brukerId, string? brukerNavn)
    {
        if (linjer.Count == 0 || linjer.Any(l => l.Antall <= 0))
        {
            throw new InvalidOperationException("Handlekurven er tom.");
        }

        if (type == KasseSalgType.Kreditnota)
        {
            throw new InvalidOperationException("Bruk KrediterAsync for kreditnota.");
        }

        rabattProsent = Math.Clamp(rabattProsent, 0, 100);
        var oppsett = await OppsettAsync();
        var varer = await db.KasseVarer.AsNoTracking().Where(v => linjer.Select(l => l.KasseVareId).Contains(v.Id)).ToDictionaryAsync(v => v.Id);
        var salg = new KasseSalg
        {
            Type = type,
            Tidspunkt = NorskTid.Na,
            RabattProsent = rabattProsent,
            KundeNavn = Ren(kundeNavn),
            KundeEpost = Ren(kundeEpost),
            Mottatt = type == KasseSalgType.Kontant ? mottatt : null,
            BrukerId = brukerId,
            BrukerNavn = brukerNavn,
            KasseNavn = oppsett.KasseNavn
        };
        foreach (var l in linjer)
        {
            var v = l.KasseVareId is { } id ? varer.GetValueOrDefault(id) : null;
            var belop = Rund(l.Enhetspris * l.Antall * (1 - rabattProsent / 100m));
            salg.Linjer.Add(new KasseSalgLinje
            {
                KasseVareId = v?.Id,
                ComponentId = v?.ComponentId,
                Navn = l.Navn,
                Kategori = l.Kategori ?? v?.Kategori,
                Antall = l.Antall,
                Enhetspris = l.Enhetspris,
                MvaSats = l.MvaSats,
                RabattProsent = rabattProsent,
                Belop = belop,
                Mva = MvaAv(belop, l.MvaSats)
            });
        }

        salg.Total = salg.Linjer.Sum(l => l.Belop);
        salg.Mva = salg.Linjer.Sum(l => l.Mva);
        if (type == KasseSalgType.Kontant && (mottatt is null || mottatt < salg.Total))
        {
            throw new InvalidOperationException("Mottatt beløp er mindre enn summen.");
        }

        await LagreMedNummerAsync(salg);
        await LagerAsync(salg, -1, $"Kassasalg {salg.Nummer}", brukerId, brukerNavn);
        return salg;
    }

    // Kreditnota for hele eller deler av et salg. linjeIder = null: hele salget.
    public async Task<KasseSalg> KrediterAsync(int salgId, IReadOnlyCollection<int>? linjeIder, string? arsak, int? brukerId, string? brukerNavn)
    {
        var opprinnelig = await db.KasseSalg.AsNoTracking().Include(s => s.Linjer).FirstOrDefaultAsync(s => s.Id == salgId)
            ?? throw new InvalidOperationException("Fant ikke salget.");
        if (opprinnelig.Type == KasseSalgType.Kreditnota)
        {
            throw new InvalidOperationException("En kreditnota kan ikke krediteres.");
        }

        // Det som allerede er kreditert av dette salget, kan ikke krediteres igjen.
        var kreditert = (await db.KasseSalgLinjer.AsNoTracking()
                .Where(l => l.KasseSalg!.KrediterSalgId == salgId)
                .Select(l => new { l.Navn, l.KasseVareId, l.Antall }).ToListAsync())
            .GroupBy(l => (l.KasseVareId, l.Navn)).ToDictionary(g => g.Key, g => -g.Sum(l => l.Antall));

        var valgte = opprinnelig.Linjer.Where(l => linjeIder is null || linjeIder.Contains(l.Id)).ToList();
        var oppsett = await OppsettAsync();
        var kred = new KasseSalg
        {
            Type = KasseSalgType.Kreditnota,
            BetaltTilbakeMed = opprinnelig.Type,
            KrediterSalgId = opprinnelig.Id,
            Arsak = Ren(arsak) ?? "Retur",
            Tidspunkt = NorskTid.Na,
            KundeNavn = opprinnelig.KundeNavn,
            KundeEpost = opprinnelig.KundeEpost,
            RabattProsent = opprinnelig.RabattProsent,
            BrukerId = brukerId,
            BrukerNavn = brukerNavn,
            KasseNavn = oppsett.KasseNavn
        };
        foreach (var l in valgte)
        {
            var igjen = l.Antall - kreditert.GetValueOrDefault((l.KasseVareId, l.Navn));
            if (igjen <= 0)
            {
                continue;
            }

            var andel = l.Antall == 0 ? 0 : (decimal)igjen / l.Antall;
            var belop = -Rund(l.Belop * andel);
            kred.Linjer.Add(new KasseSalgLinje
            {
                KasseVareId = l.KasseVareId,
                ComponentId = l.ComponentId,
                Navn = l.Navn,
                Kategori = l.Kategori,
                Antall = -igjen,
                Enhetspris = l.Enhetspris,
                MvaSats = l.MvaSats,
                RabattProsent = l.RabattProsent,
                Belop = belop,
                Mva = MvaAv(belop, l.MvaSats)
            });
        }

        if (kred.Linjer.Count == 0)
        {
            throw new InvalidOperationException("Alt i salget er allerede kreditert.");
        }

        kred.Total = kred.Linjer.Sum(l => l.Belop);
        kred.Mva = kred.Linjer.Sum(l => l.Mva);
        await LagreMedNummerAsync(kred);
        await LagerAsync(kred, -1, $"Kreditnota {kred.Nummer} (retur)", brukerId, brukerNavn);
        return kred;
    }

    // Neste nummer i serien. Den unike indeksen hindrer dobbeltnummer hvis to
    // kasser lagrer samtidig; da prøves neste nummer.
    private async Task LagreMedNummerAsync(KasseSalg salg)
    {
        for (var forsok = 0; ; forsok++)
        {
            salg.Nummer = (await db.KasseSalg.MaxAsync(s => (int?)s.Nummer) ?? 1000) + 1;
            db.KasseSalg.Add(salg);
            try
            {
                await db.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException) when (forsok < 4)
            {
                db.Entry(salg).State = EntityState.Detached;
                foreach (var l in salg.Linjer)
                {
                    db.Entry(l).State = EntityState.Detached;
                }
            }
        }
    }

    // Varer koblet til komponentregisteret trekkes fra (eller legges tilbake
    // på) hovedlageret når organisasjonen har lagermodulen.
    private async Task LagerAsync(KasseSalg salg, int fortegn, string tekst, int? brukerId, string? brukerNavn)
    {
        var aktuelle = salg.Linjer.Where(l => l.ComponentId is not null).ToList();
        if (aktuelle.Count == 0 || !await tilleggsmoduler.HarAsync(TilleggsModuler.Lager))
        {
            return;
        }

        var ider = aktuelle.Select(l => l.ComponentId!.Value).Distinct().ToList();
        var styrt = await db.Components.AsNoTracking().Where(c => ider.Contains(c.Id) && c.ILagerstyring).Select(c => c.Id).ToListAsync();
        if (styrt.Count == 0)
        {
            return;
        }

        var hoved = await lager.HovedlagerAsync();
        foreach (var l in aktuelle.Where(l => styrt.Contains(l.ComponentId!.Value)))
        {
            await lager.RegistrerAsync(l.ComponentId!.Value, hoved.Id, fortegn * l.Antall,
                l.Antall > 0 ? LagerBevegelseType.Uttak : LagerBevegelseType.Tilgang, tekst, brukerId, brukerNavn);
        }
    }

    // ---------- Oppgjør ----------

    public static DagTall Summer(IReadOnlyCollection<KasseSalg> salg)
    {
        var vanlige = salg.Where(s => s.Type != KasseSalgType.Kreditnota).ToList();
        var kred = salg.Where(s => s.Type == KasseSalgType.Kreditnota).ToList();
        return new DagTall(
            vanlige.Count, kred.Count,
            salg.Sum(s => s.Total), salg.Sum(s => s.Mva),
            vanlige.Where(s => s.Type == KasseSalgType.Kort).Sum(s => s.Total),
            vanlige.Where(s => s.Type == KasseSalgType.Kontant).Sum(s => s.Total),
            kred.Where(s => s.BetaltTilbakeMed == KasseSalgType.Kort).Sum(s => s.Total),
            kred.Where(s => s.BetaltTilbakeMed != KasseSalgType.Kort).Sum(s => s.Total),
            salg.Count == 0 ? null : salg.Min(s => s.Nummer), salg.Count == 0 ? null : salg.Max(s => s.Nummer));
    }

    public static List<Mvalinje> MvaFordeling(IEnumerable<KasseSalgLinje> linjer) =>
        linjer.GroupBy(l => l.MvaSats).OrderByDescending(g => g.Key)
            .Select(g => new Mvalinje(g.Key, g.Sum(l => l.Belop) - g.Sum(l => l.Mva), g.Sum(l => l.Mva))).ToList();

    public Task<List<KasseSalg>> ApneSalgAsync() =>
        db.KasseSalg.AsNoTracking().Include(s => s.Linjer).Where(s => s.DagsoppgjorId == null).OrderBy(s => s.Nummer).ToListAsync();

    public async Task<decimal> VekselStartAsync()
    {
        var oppsett = await OppsettAsync();
        return oppsett.Vekselbeholdning;
    }

    public async Task<KasseDagsoppgjor> LukkDagAsync(decimal terminalTotal, IReadOnlyDictionary<int, int> telling, decimal vekselStart, string? forklaring, string? lukketAv)
    {
        var salg = await db.KasseSalg.Include(s => s.Linjer).Where(s => s.DagsoppgjorId == null).OrderBy(s => s.Nummer).ToListAsync();
        var t = Summer(salg);
        var opptalt = telling.Sum(x => (decimal)x.Key * x.Value);
        var difKort = Rund(terminalTotal - t.NettoKort);
        var difKontant = Rund(opptalt - (vekselStart + t.NettoKontant));
        if ((Math.Abs(difKort) >= 0.5m || Math.Abs(difKontant) >= 0.5m) && string.IsNullOrWhiteSpace(forklaring))
        {
            throw new InvalidOperationException("Avvik må forklares før dagen lukkes.");
        }

        var oppsett = await OppsettAsync();
        var oppgjor = new KasseDagsoppgjor
        {
            Lukket = NorskTid.Na,
            LukketAv = lukketAv,
            KasseNavn = oppsett.KasseNavn,
            FraNummer = t.FraNummer,
            TilNummer = t.TilNummer,
            AntallSalg = t.AntallSalg,
            AntallKreditnota = t.AntallKreditnota,
            Omsetning = t.Omsetning,
            Mva = t.Mva,
            KortIKassa = t.NettoKort,
            KontantIKassa = t.NettoKontant,
            TerminalTotal = terminalTotal,
            VekselStart = vekselStart,
            Opptalt = opptalt,
            TellingJson = JsonSerializer.Serialize(telling.Where(x => x.Value > 0).ToDictionary(x => x.Key.ToString(CultureInfo.InvariantCulture), x => x.Value)),
            DifferanseKort = difKort,
            DifferanseKontant = difKontant,
            Forklaring = Ren(forklaring)
        };
        db.KasseDagsoppgjor.Add(oppgjor);
        foreach (var s in salg)
        {
            s.Dagsoppgjor = oppgjor;
        }

        await db.SaveChangesAsync();
        return oppgjor;
    }

    // ---------- Bankavstemming ----------

    public sealed record BankPost(DateTime Dato, decimal Belop, string Tekst);

    // Leser en kontoutskrift (CSV) fra nettbanken. Formatene varierer, så
    // dato, beløp og tekst finnes per linje: første dato, siste beløp og
    // lengste tekstfelt. Bare innbetalinger (positive beløp) tas med.
    public static List<BankPost> LesKontoutskrift(string csv)
    {
        var poster = new List<BankPost>();
        foreach (var rå in csv.Split('\n'))
        {
            var linje = rå.Trim().Trim('\r');
            if (linje.Length == 0)
            {
                continue;
            }

            var skille = linje.Count(c => c == ';') >= linje.Count(c => c == '\t') ? (linje.Contains(';') ? ';' : ',') : '\t';
            var felt = linje.Split(skille).Select(f => f.Trim().Trim('"').Trim()).ToList();
            DateTime? dato = null;
            decimal? belop = null;
            foreach (var f in felt)
            {
                if (dato is null && DateTime.TryParseExact(f, ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd.MM.yy", "dd/MM/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    dato = d;
                }
                else if (Belop().IsMatch(f) && decimal.TryParse(NormaliserBelop(f), NumberStyles.Number, CultureInfo.InvariantCulture, out var b))
                {
                    belop = b;
                }
            }

            if (dato is { } dt && belop is { } bb && bb > 0)
            {
                var tekst = felt.Where(f => !Belop().IsMatch(f)).OrderByDescending(f => f.Length).FirstOrDefault() ?? "";
                poster.Add(new BankPost(dt, bb, tekst));
            }
        }

        return poster;
    }

    // Kobler innbetalinger til lukkede dager uten utbetaling: samme beløp som
    // kortsalget, 0-5 dager etter at dagen ble lukket. Returnerer antall koblet.
    public async Task<int> KobleBankAsync(IReadOnlyList<BankPost> poster)
    {
        var apne = await db.KasseDagsoppgjor.Where(o => o.UtbetaltBank == null && o.KortIKassa > 0).OrderBy(o => o.Lukket).ToListAsync();
        var brukt = new HashSet<BankPost>();
        var antall = 0;
        foreach (var o in apne)
        {
            var treff = poster.Where(p => !brukt.Contains(p) && Math.Abs(p.Belop - o.KortIKassa) < 0.5m
                    && p.Dato.Date >= o.Lukket.Date && p.Dato.Date <= o.Lukket.Date.AddDays(5))
                .OrderBy(p => p.Dato).FirstOrDefault()
                // Uten eksakt treff: innbetaling fra innløser samme periode med beløp nær kortsummen.
                ?? poster.Where(p => !brukt.Contains(p) && p.Dato.Date >= o.Lukket.Date && p.Dato.Date <= o.Lukket.Date.AddDays(5)
                        && Innloser().IsMatch(p.Tekst) && Math.Abs(p.Belop - o.KortIKassa) <= Math.Max(500m, o.KortIKassa * 0.05m))
                    .OrderBy(p => Math.Abs(p.Belop - o.KortIKassa)).FirstOrDefault();
            if (treff is not null)
            {
                brukt.Add(treff);
                o.UtbetaltBank = treff.Belop;
                antall++;
            }
        }

        await db.SaveChangesAsync();
        return antall;
    }

    // «1 234,50» og «1.234,50» (norsk) eller «1234.50».
    private static string NormaliserBelop(string f)
    {
        var t = f.Replace(" ", "").Replace(" ", "");
        return t.Contains(',') ? t.Replace(".", "").Replace(",", ".") : t;
    }

    private static string? Ren(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [GeneratedRegex(@"^-?[\d\s .]*\d,\d{2}$|^-?\d+(\.\d{2})$")]
    private static partial Regex Belop();

    [GeneratedRegex(@"nets|nexi|bambora|worldline|verifone|swedbank pay|teller|elavon|izettle|zettle|sumup|innløser|kortoppgjør|terminal", RegexOptions.IgnoreCase)]
    private static partial Regex Innloser();
}
