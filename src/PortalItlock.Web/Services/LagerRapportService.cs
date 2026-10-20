using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Lagerstyring fase 5: økonomi og analyse.
//
// Alt regnes ut fra lagerbevegelsene (som har kost per stk på tidspunktet),
// reservasjonene og innkjøpsordrene - ingen egne tabeller.
public class LagerRapportService(ApplicationDbContext db)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    // ---------- Prosjektøkonomi ----------

    public sealed record ProsjektKost(string Nokkel, string Navn, string? Undertekst, string Lenke, int Stk,
        decimal Forbrukt, decimal IBil, decimal Reservert, DateTime? SistForbruk)
    {
        public decimal Totalt => Forbrukt + IBil + Reservert;
    }

    public async Task<List<ProsjektKost>> ProsjektKostAsync()
    {
        var forbruk = await db.LagerBevegelser.AsNoTracking()
            .Where(b => b.Type == LagerBevegelseType.Forbruk && (b.ProsjektId != null || b.ArbeidsordreId != null))
            .Select(b => new { b.ProsjektId, b.ArbeidsordreId, b.Antall, b.KostPerStk, b.Tidspunkt }).ToListAsync();
        var reservasjoner = await db.LagerReservasjoner.AsNoTracking().Include(r => r.Component)
            .Where(r => r.Antall > 0).ToListAsync();

        // Forbruk på en arbeidsordre som hører til et prosjekt, regnes på
        // prosjektet (ProsjektId er da satt på bevegelsen).
        string Nokkel(int? prosjektId, int? arbeidsordreId) => prosjektId is { } p ? $"p{p}" : $"a{arbeidsordreId}";

        var nokler = forbruk.Select(f => Nokkel(f.ProsjektId, f.ArbeidsordreId))
            .Concat(reservasjoner.Select(r => Nokkel(r.ProsjektId, r.ArbeidsordreId))).Distinct().ToList();
        var prosjektIder = nokler.Where(n => n[0] == 'p').Select(n => int.Parse(n[1..])).ToList();
        var aoIder = nokler.Where(n => n[0] == 'a').Select(n => int.Parse(n[1..])).ToList();
        var prosjekter = await db.Prosjekter.AsNoTracking().Include(p => p.Kunde).Where(p => prosjektIder.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var arbeidsordre = await db.Arbeidsordre.AsNoTracking().Include(a => a.Kunde).Where(a => aoIder.Contains(a.Id)).ToDictionaryAsync(a => a.Id);

        var resultat = new List<ProsjektKost>();
        foreach (var n in nokler)
        {
            var id = int.Parse(n[1..]);
            var f = forbruk.Where(x => Nokkel(x.ProsjektId, x.ArbeidsordreId) == n).ToList();
            var r = reservasjoner.Where(x => Nokkel(x.ProsjektId, x.ArbeidsordreId) == n).ToList();
            decimal Kost(LagerReservasjon x) => x.Component is { } c ? LagerService.KostFor(c) : 0m;

            string navn, lenke;
            string? under;
            if (n[0] == 'p')
            {
                var p = prosjekter.GetValueOrDefault(id);
                navn = p?.Navn ?? $"Prosjekt {id}";
                under = p?.Kunde?.Navn;
                lenke = $"/prosjekter/{id}";
            }
            else
            {
                var a = arbeidsordre.GetValueOrDefault(id);
                navn = a?.Tittel ?? $"Arbeidsordre {id}";
                under = "Arbeidsordre" + (a?.Kunde is { } k ? $" · {k.Navn}" : "");
                lenke = $"/arbeidsordre/{id}";
            }

            resultat.Add(new ProsjektKost(n, navn, under, lenke,
                -f.Sum(x => x.Antall),
                -f.Sum(x => x.Antall * x.KostPerStk),
                r.Sum(x => x.Plukket * Kost(x)),
                r.Sum(x => (x.Antall - x.Plukket) * Kost(x)),
                f.Count == 0 ? null : f.Max(x => x.Tidspunkt)));
        }

        return resultat.OrderByDescending(r => r.Totalt).ToList();
    }

    // ---------- Brukt, ikke fakturert (kontroll før Tripletex) ----------

    public sealed record Mangel(int ComponentId, string Navn, int Brukt, int PaOrdren, decimal Kost, decimal Utpris);

    public sealed record IkkeFakturert(int ArbeidsordreId, string Tittel, string? Kunde, ArbeidsordreStatus Status,
        decimal Forbrukt, bool SendtTilTripletex, List<Mangel> Mangler);

    // Arbeidsordre med lagerforbruk som ikke er fakturert. Mangler = varer
    // som er tatt fra lager/bil mot ordren, men ikke ligger på ordren (og
    // derfor ikke blir fakturert).
    public async Task<List<IkkeFakturert>> IkkeFakturertAsync()
    {
        var forbruk = await db.LagerBevegelser.AsNoTracking().Include(b => b.Component)
            .Where(b => b.Type == LagerBevegelseType.Forbruk && b.ArbeidsordreId != null)
            .ToListAsync();
        var ider = forbruk.Select(b => b.ArbeidsordreId!.Value).Distinct().ToList();
        var ordre = await db.Arbeidsordre.AsNoTracking().Include(a => a.Kunde)
            .Where(a => ider.Contains(a.Id) && a.Status != ArbeidsordreStatus.Fakturert && a.FakturertDato == null)
            .ToListAsync();
        var paOrdren = (await db.ArbeidsordreVarer.AsNoTracking()
                .Where(v => ider.Contains(v.ArbeidsordreId) && v.ComponentId != null)
                .Select(v => new { v.ArbeidsordreId, ComponentId = v.ComponentId!.Value, v.Antall }).ToListAsync())
            .GroupBy(v => (v.ArbeidsordreId, v.ComponentId)).ToDictionary(g => g.Key, g => g.Sum(v => v.Antall));

        return ordre.Select(a =>
        {
            var f = forbruk.Where(b => b.ArbeidsordreId == a.Id).ToList();
            var mangler = f.GroupBy(b => b.ComponentId)
                .Select(g => new Mangel(g.Key, g.First().Component?.Navn ?? "Vare", -g.Sum(b => b.Antall),
                    paOrdren.GetValueOrDefault((a.Id, g.Key)), g.First().KostPerStk, g.First().Component?.PrisVeiledende ?? 0m))
                .Where(m => m.Brukt > m.PaOrdren)
                .ToList();
            return new IkkeFakturert(a.Id, a.Tittel, a.Kunde?.Navn, a.Status, -f.Sum(b => b.Antall * b.KostPerStk),
                a.TripletexOrdreId != null, mangler);
        })
        .OrderByDescending(x => x.Mangler.Count > 0).ThenByDescending(x => x.Status == ArbeidsordreStatus.Ferdig).ThenBy(x => x.Tittel)
        .ToList();
    }

    // Legger varer som er brukt, men mangler på arbeidsordren, inn på ordren.
    public async Task<int> LeggManglerPaOrdrenAsync(IkkeFakturert ao, int? brukerId)
    {
        foreach (var m in ao.Mangler)
        {
            db.ArbeidsordreVarer.Add(new ArbeidsordreVare
            {
                ArbeidsordreId = ao.ArbeidsordreId,
                ComponentId = m.ComponentId,
                Navn = m.Navn,
                Antall = m.Brukt - m.PaOrdren,
                Kostpris = m.Kost,
                Utpris = m.Utpris,
                LagtTilAvBrukerId = brukerId,
                LagtTilDato = DateTime.Now,
                Plukket = true,
                PlukketDato = DateTime.Now,
                PlukketAvBrukerId = brukerId
            });
        }

        await db.SaveChangesAsync();
        return ao.Mangler.Sum(m => m.Brukt - m.PaOrdren);
    }

    // Forbruk og uttak som ikke er knyttet til noen jobb (følges opp - kan
    // være glemt fakturering eller svinn).
    public async Task<List<LagerBevegelse>> UtenJobbAsync(int dager = 60)
    {
        var fra = DateTime.Today.AddDays(-dager);
        return await db.LagerBevegelser.AsNoTracking().Include(b => b.Component).Include(b => b.Lager)
            .Where(b => (b.Type == LagerBevegelseType.Forbruk || b.Type == LagerBevegelseType.Uttak)
                && b.ProsjektId == null && b.ArbeidsordreId == null && b.Tidspunkt >= fra)
            .OrderByDescending(b => b.Tidspunkt).Take(200).ToListAsync();
    }

    // ---------- Lagerverdi over tid ----------

    // Verdi ved utgangen av hver måned: summen av alle bevegelser til og med
    // måneden (antall × kost på tidspunktet). Flytting mellom lagre går i null.
    public async Task<List<(DateTime Maaned, decimal Verdi, decimal Inn, decimal Ut)>> LagerverdiAsync(int maaneder = 12)
    {
        var bevegelser = await db.LagerBevegelser.AsNoTracking()
            .Select(b => new { b.Tidspunkt, b.Antall, b.KostPerStk, b.Type }).ToListAsync();
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(maaneder - 1));
        if (bevegelser.Count > 0)
        {
            // Ikke vis tomme måneder fra før lagermodulen ble tatt i bruk.
            var forste = bevegelser.Min(b => b.Tidspunkt);
            var forsteMnd = new DateTime(forste.Year, forste.Month, 1);
            if (forsteMnd > start)
            {
                start = forsteMnd;
            }
        }

        var resultat = new List<(DateTime, decimal, decimal, decimal)>();
        for (var m = start; m <= DateTime.Today; m = m.AddMonths(1))
        {
            var slutt = m.AddMonths(1);
            var iMnd = bevegelser.Where(b => b.Tidspunkt >= m && b.Tidspunkt < slutt
                && b.Type != LagerBevegelseType.FlyttInn && b.Type != LagerBevegelseType.FlyttUt).ToList();
            resultat.Add((m,
                bevegelser.Where(b => b.Tidspunkt < slutt).Sum(b => b.Antall * b.KostPerStk),
                iMnd.Where(b => b.Antall > 0).Sum(b => b.Antall * b.KostPerStk),
                -iMnd.Where(b => b.Antall < 0).Sum(b => b.Antall * b.KostPerStk)));
        }

        return resultat;
    }

    // ---------- Døde varer ----------

    public sealed record DodVare(Component Vare, int Antall, decimal Verdi, DateTime? SistBrukt, int? Dager);

    // Lagerførte varer med beholdning som ikke er brukt (forbruk/uttak) på
    // minst «dager» dager - eller aldri.
    public async Task<List<DodVare>> DodeVarerAsync(int dager)
    {
        var varer = await db.Components.AsNoTracking().Where(c => c.ILagerstyring && c.Lagerantall > 0).ToListAsync();
        var ider = varer.Select(c => c.Id).ToList();
        var sist = (await db.LagerBevegelser.AsNoTracking()
                .Where(b => ider.Contains(b.ComponentId) && (b.Type == LagerBevegelseType.Forbruk || b.Type == LagerBevegelseType.Uttak))
                .GroupBy(b => b.ComponentId).Select(g => new { g.Key, Sist = g.Max(b => b.Tidspunkt) }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.Sist);
        var grense = DateTime.Today.AddDays(-dager);

        return varer
            .Select(c =>
            {
                DateTime? s = sist.TryGetValue(c.Id, out var d) ? d : null;
                return new DodVare(c, c.Lagerantall, c.Lagerantall * LagerService.KostFor(c), s, s is { } x ? (DateTime.Today - x.Date).Days : null);
            })
            .Where(d => d.SistBrukt is null || d.SistBrukt < grense)
            .OrderByDescending(d => d.Verdi).ToList();
    }

    // ---------- Leverandører ----------

    public sealed record LeverandorStat(string Navn, int Ordre, decimal Verdi, int MedAvvik, decimal PrisAvvik,
        int Levert, int ITide, double? SnittDagerForsinket, double? SnittLeveringstid);

    public async Task<List<LeverandorStat>> LeverandorerAsync()
    {
        var ordre = await db.Innkjopsordrer.AsNoTracking().Include(o => o.Linjer)
            .Where(o => o.Status != InnkjopStatus.Utkast && o.Status != InnkjopStatus.Kansellert).ToListAsync();
        var ider = ordre.Select(o => o.Id).ToList();
        var forsteMottak = (await db.LagerBevegelser.AsNoTracking()
                .Where(b => b.Type == LagerBevegelseType.Mottak && b.InnkjopsordreId != null && ider.Contains(b.InnkjopsordreId!.Value))
                .GroupBy(b => b.InnkjopsordreId!.Value).Select(g => new { g.Key, Forste = g.Min(b => b.Tidspunkt) }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.Forste);

        return ordre.GroupBy(o => o.LeverandorNavn)
            .Select(g =>
            {
                var levert = g.Where(o => forsteMottak.ContainsKey(o.Id)).ToList();
                var medFrist = levert
                    .Select(o => (Mottak: forsteMottak[o.Id].Date, Frist: (o.Linjer.Max(l => l.BekreftetLevering) ?? o.OnsketLevering)?.Date, Sendt: o.Sendt?.Date))
                    .ToList();
                var sene = medFrist.Where(x => x.Frist is not null).Select(x => Math.Max(0, (x.Mottak - x.Frist!.Value).Days)).ToList();
                var tider = medFrist.Where(x => x.Sendt is not null).Select(x => (double)(x.Mottak - x.Sendt!.Value).Days).ToList();
                return new LeverandorStat(g.Key, g.Count(), g.Sum(o => o.Linjer.Sum(l => l.Antall * l.Pris)),
                    g.Count(o => o.HaddeAvvik || o.Status == InnkjopStatus.Avvik), g.Sum(o => o.AvvikPris ?? 0m),
                    levert.Count, medFrist.Count(x => x.Frist is null || x.Mottak <= x.Frist),
                    sene.Count == 0 ? null : sene.Average(), tider.Count == 0 ? null : tider.Average());
            })
            .OrderByDescending(s => s.Verdi).ToList();
    }

    // ---------- Eksport (CSV for regnskap / Tripletex-kontroll) ----------

    private static string Csv(string? s) => s is null ? "" : s.Contains(';') || s.Contains('"') || s.Contains('\n') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    private static string Tall(decimal d) => d.ToString("0.00", Kultur);

    private static string TypeNavn(LagerBevegelseType t) => t switch
    {
        LagerBevegelseType.Inngaende => "Startbeholdning",
        LagerBevegelseType.Tilgang => "Tilgang",
        LagerBevegelseType.Uttak => "Uttak",
        LagerBevegelseType.FlyttUt => "Flyttet ut",
        LagerBevegelseType.FlyttInn => "Flyttet inn",
        LagerBevegelseType.Telling => "Varetelling",
        LagerBevegelseType.Korrigering => "Korrigering",
        LagerBevegelseType.Mottak => "Varemottak",
        LagerBevegelseType.Forbruk => "Forbruk",
        LagerBevegelseType.Retur => "Retur",
        _ => t.ToString()
    };

    public async Task<byte[]> BeholdningCsvAsync()
    {
        var beholdning = await db.LagerBevegelser.AsNoTracking()
            .GroupBy(b => new { b.ComponentId, b.LagerId }).Select(g => new { g.Key.ComponentId, g.Key.LagerId, Antall = g.Sum(b => b.Antall) })
            .Where(b => b.Antall != 0).ToListAsync();
        var varer = await db.Components.AsNoTracking().Where(c => beholdning.Select(b => b.ComponentId).Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        var lagre = await db.Lagre.AsNoTracking().ToDictionaryAsync(l => l.Id);

        var sb = new StringBuilder("Varenr;Vare;Leverandør;Lager;Antall;Enhet;Snittkost;Verdi\n");
        foreach (var b in beholdning.OrderBy(b => varer.GetValueOrDefault(b.ComponentId)?.Navn).ThenBy(b => b.LagerId))
        {
            var c = varer.GetValueOrDefault(b.ComponentId);
            var kost = c is null ? 0m : LagerService.KostFor(c);
            sb.Append($"{Csv(c?.Produktkode)};{Csv(c?.Navn)};{Csv(c?.Leverandor)};{Csv(lagre.GetValueOrDefault(b.LagerId)?.Navn)};{b.Antall};{Csv(c?.Enhet ?? "stk")};{Tall(kost)};{Tall(b.Antall * kost)}\n");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> BevegelserCsvAsync(DateTime fra, DateTime til)
    {
        var rader = await db.LagerBevegelser.AsNoTracking().Include(b => b.Component).Include(b => b.Lager)
            .Where(b => b.Tidspunkt >= fra && b.Tidspunkt < til.AddDays(1))
            .OrderBy(b => b.Tidspunkt).ToListAsync();
        var prosjektIder = rader.Where(r => r.ProsjektId != null).Select(r => r.ProsjektId!.Value).Distinct().ToList();
        var prosjekter = await db.Prosjekter.AsNoTracking().Where(p => prosjektIder.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Navn);
        var ordre = await db.Innkjopsordrer.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Visningsnummer);

        var sb = new StringBuilder("Tidspunkt;Type;Varenr;Vare;Lager;Antall;Kost per stk;Verdi;Prosjekt;Arbeidsordre;Innkjøpsordre;Bruker;Notat\n");
        foreach (var b in rader)
        {
            sb.Append($"{b.Tidspunkt:dd.MM.yyyy HH:mm};{TypeNavn(b.Type)};{Csv(b.Component?.Produktkode)};{Csv(b.Component?.Navn)};{Csv(b.Lager?.Navn)};{b.Antall};{Tall(b.KostPerStk)};{Tall(b.Antall * b.KostPerStk)};"
                + $"{Csv(b.ProsjektId is { } p ? prosjekter.GetValueOrDefault(p) : null)};{b.ArbeidsordreId};{Csv(b.InnkjopsordreId is { } o ? ordre.GetValueOrDefault(o) : null)};{Csv(b.BrukerNavn)};{Csv(b.Notat)}\n");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
