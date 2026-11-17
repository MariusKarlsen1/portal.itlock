using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Ressursplan: timer per uke fremover per prosjekt, mot manuell kapasitet.
// Prosjektene hentes inn av seg selv (SynkAsync): registrerte og pågående som
// «Mindre prosjekter», tilbud som «Potensielle». Avslåtte tas ut, og godkjente
// tilbud flyttes fra Potensielle. Hver endring havner under «Endringer».
public class UkeplanService(ApplicationDbContext db)
{
    public readonly record struct Uke(int Ar, int Nr)
    {
        public DateTime Mandag => ISOWeek.ToDateTime(Ar, Nr, DayOfWeek.Monday);
        public string Nokkel => $"{Ar}-{Nr}";
        public static Uke For(DateTime dato) => new(ISOWeek.GetYear(dato), ISOWeek.GetWeekOfYear(dato));
        public Uke Pluss(int uker) => For(Mandag.AddDays(7 * uker));
        public int CompareTo(Uke o) => Ar != o.Ar ? Ar.CompareTo(o.Ar) : Nr.CompareTo(o.Nr);
    }

    public static List<Uke> Uker(Uke fra, int antall) => Enumerable.Range(0, antall).Select(fra.Pluss).ToList();

    private static readonly ProsjektStatus[] Pagaende = [ProsjektStatus.Registrert, ProsjektStatus.UnderArbeid, ProsjektStatus.Aktiv];

    public static bool ErPagaende(ProsjektStatus? s) => s is { } v && Pagaende.Contains(v);

    // Henter inn nye prosjekter og følger statusendringer. Første gang legges alt
    // inn uten å fylle «Endringer».
    public async Task SynkAsync()
    {
        var forsteGang = !await db.UkeplanRader.AnyAsync();
        var prosjekter = await db.Prosjekter.AsNoTracking()
            .Where(p => p.Status != null && p.Status != ProsjektStatus.Serviceavtale && !p.ErServiceoppdrag)
            .Select(p => new { p.Id, p.Navn, p.Status, p.ProsjektlederId })
            .ToListAsync();
        var rader = await db.UkeplanRader.Where(r => r.ProsjektId != null).ToListAsync();
        var perProsjekt = rader.GroupBy(r => r.ProsjektId!.Value).ToDictionary(g => g.Key, g => g.First());
        var endret = false;

        foreach (var p in prosjekter)
        {
            perProsjekt.TryGetValue(p.Id, out var rad);
            if (rad is null)
            {
                if (!ErPagaende(p.Status) && p.Status != ProsjektStatus.Tilbud)
                {
                    continue;
                }

                rad = new UkeplanRad
                {
                    ProsjektId = p.Id,
                    Kategori = p.Status == ProsjektStatus.Tilbud ? UkeplanKategori.Potensiell : UkeplanKategori.Mindre,
                    AnsvarligId = p.ProsjektlederId
                };
                db.UkeplanRader.Add(rad);
                if (!forsteGang && ErPagaende(p.Status))
                {
                    db.UkeplanEndringer.Add(new UkeplanEndring
                    {
                        Type = UkeplanEndringType.Ny,
                        Rad = rad,
                        Tekst = p.Navn,
                        Under = $"{p.Status!.Value.Visningsnavn()} · ikke fordelt på uker ennå"
                    });
                }
                endret = true;
                continue;
            }

            if (p.Status == ProsjektStatus.TilbudAvslatt && !rad.Skjult)
            {
                rad.Skjult = true;
                db.UkeplanEndringer.Add(new UkeplanEndring
                {
                    Type = UkeplanEndringType.Avslatt,
                    RadId = rad.Id,
                    Tekst = p.Navn,
                    Under = "Tilbudet er avslått · fjernet fra planen"
                });
                endret = true;
            }
            else if (rad.Kategori == UkeplanKategori.Potensiell && ErPagaende(p.Status))
            {
                rad.Kategori = UkeplanKategori.Mindre;
                db.UkeplanEndringer.Add(new UkeplanEndring
                {
                    Type = UkeplanEndringType.Godkjent,
                    RadId = rad.Id,
                    Tekst = p.Navn,
                    Under = $"Satt til {p.Status!.Value.Visningsnavn()} · flyttet fra Potensielle"
                });
                endret = true;
            }
        }

        if (endret)
        {
            await db.SaveChangesAsync();
        }
    }

    // Kalkulert montasjetid fra Montering (uten opsjoner), i timer per prosjekt.
    public async Task<Dictionary<int, decimal>> KalkulertAsync(IReadOnlyCollection<int> prosjektIder) =>
        (await db.MonteringLinjer.AsNoTracking()
            .Where(m => prosjektIder.Contains(m.ProsjektId) && !m.ErOpsjon)
            .Select(m => new { m.ProsjektId, m.Minutter, m.Antall })
            .ToListAsync())
        .GroupBy(m => m.ProsjektId)
        .ToDictionary(g => g.Key, g => g.Sum(m => (decimal)(m.Minutter ?? 0) * m.Antall) / 60m);

    // Timer ført på arbeidsordre i prosjektet.
    public async Task<Dictionary<int, decimal>> BruktAsync(IReadOnlyCollection<int> prosjektIder) =>
        (await db.Timeregistreringer.AsNoTracking()
            .Where(t => t.Arbeidsordre!.ProsjektId != null && prosjektIder.Contains(t.Arbeidsordre.ProsjektId!.Value))
            .Select(t => new { ProsjektId = t.Arbeidsordre!.ProsjektId!.Value, t.Start, t.Slutt, t.PauseMinutter })
            .ToListAsync())
        .GroupBy(t => t.ProsjektId)
        .ToDictionary(g => g.Key, g => g.Sum(t => Math.Max(0m, (decimal)(t.Slutt - t.Start).TotalMinutes - t.PauseMinutter)) / 60m);

    // Standard kapasitet per uke. Er den ikke satt, foreslås aktive montører × 37,5 t.
    public async Task<decimal> StandardKapasitetAsync()
    {
        var std = await db.UkeplanKapasitet.AsNoTracking().FirstOrDefaultAsync(k => k.Ar == 0 && k.Uke == 0);
        if (std is not null)
        {
            return std.Timer;
        }

        var montorer = await db.Brukere.CountAsync(b => b.Rolle == BrukerRolle.Montor && b.Aktiv && b.SlettetDato == null && !b.ErSkjult);
        return Math.Max(1, montorer) * 37.5m;
    }

    public async Task SettKapasitetAsync(int ar, int uke, decimal? timer)
    {
        var k = await db.UkeplanKapasitet.FirstOrDefaultAsync(x => x.Ar == ar && x.Uke == uke);
        if (timer is null)
        {
            if (k is not null)
            {
                db.UkeplanKapasitet.Remove(k);
            }
        }
        else if (k is null)
        {
            db.UkeplanKapasitet.Add(new UkeplanKapasitet { Ar = ar, Uke = uke, Timer = timer.Value });
        }
        else
        {
            k.Timer = timer.Value;
        }
        await db.SaveChangesAsync();
    }

    public static string RadNavn(UkeplanRad r) => r.Prosjekt?.Navn ?? r.Navn ?? "Uten navn";

    public static string KategoriNavn(UkeplanKategori k) => k switch
    {
        UkeplanKategori.Store => "Store prosjekter",
        UkeplanKategori.Mindre => "Mindre prosjekter",
        UkeplanKategori.Service => "Serviceavtaler / rammeavtaler",
        _ => "Potensielle prosjekter"
    };

    // Excel i samme oppsett som regnearket: grupper, prosjekt, nr, ansvarlig og én kolonne per uke.
    public async Task<byte[]> ExcelAsync(int antallUker)
    {
        var fra = Uke.For(DateTime.Today).Pluss(-2);
        var uker = Uker(fra, antallUker);
        var rader = await db.UkeplanRader.AsNoTracking()
            .Include(r => r.Prosjekt).Include(r => r.Ansvarlig).Include(r => r.Timer)
            .Where(r => !r.Skjult)
            .ToListAsync();
        var std = await StandardKapasitetAsync();
        var egne = await db.UkeplanKapasitet.AsNoTracking().Where(k => k.Ar != 0).ToListAsync();
        decimal Kap(Uke u) => egne.FirstOrDefault(k => k.Ar == u.Ar && k.Uke == u.Nr)?.Timer ?? std;

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Ressursplan");
        ws.Cell(1, 1).Value = "Prosjekt";
        ws.Cell(1, 2).Value = "Nr";
        ws.Cell(1, 3).Value = "Ansvarlig";
        for (var i = 0; i < uker.Count; i++)
        {
            ws.Cell(1, 4 + i).Value = $"UKE {uker[i].Nr}";
        }
        var hode = ws.Range(1, 1, 1, 3 + uker.Count);
        hode.Style.Font.Bold = true;
        hode.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1EC");

        var rad = 2;
        foreach (var kat in Enum.GetValues<UkeplanKategori>())
        {
            var iKat = rader.Where(r => r.Kategori == kat).OrderBy(RadNavn, StringComparer.CurrentCultureIgnoreCase).ToList();
            if (iKat.Count == 0)
            {
                continue;
            }
            ws.Cell(rad, 1).Value = KategoriNavn(kat);
            var katRange = ws.Range(rad, 1, rad, 3 + uker.Count);
            katRange.Style.Font.Bold = true;
            katRange.Style.Font.FontColor = XLColor.White;
            katRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#14213D");
            rad++;
            foreach (var r in iKat)
            {
                ws.Cell(rad, 1).Value = RadNavn(r) + (kat == UkeplanKategori.Potensiell ? $" ({r.Sannsynlighet} %)" : "");
                ws.Cell(rad, 2).Value = r.ProsjektId is { } pid ? $"P-{pid}" : "";
                ws.Cell(rad, 3).Value = r.Ansvarlig?.Navn ?? "";
                for (var i = 0; i < uker.Count; i++)
                {
                    var t = r.Timer.FirstOrDefault(x => x.Ar == uker[i].Ar && x.Uke == uker[i].Nr)?.Timer;
                    if (t is > 0)
                    {
                        ws.Cell(rad, 4 + i).Value = t.Value;
                    }
                }
                rad++;
            }
        }

        rad++;
        ws.Cell(rad, 1).Value = "Planlagt (uten potensielle)";
        ws.Cell(rad + 1, 1).Value = "Kapasitet";
        ws.Cell(rad + 2, 1).Value = "Ledig";
        for (var i = 0; i < uker.Count; i++)
        {
            var u = uker[i];
            var plan = rader.Where(r => r.Kategori != UkeplanKategori.Potensiell)
                .Sum(r => r.Timer.FirstOrDefault(x => x.Ar == u.Ar && x.Uke == u.Nr)?.Timer ?? 0);
            ws.Cell(rad, 4 + i).Value = plan;
            ws.Cell(rad + 1, 4 + i).Value = Kap(u);
            ws.Cell(rad + 2, 4 + i).Value = Kap(u) - plan;
            ws.Cell(rad + 2, 4 + i).Style.Font.FontColor = Kap(u) - plan < 0 ? XLColor.FromHtml("#B3261E") : XLColor.FromHtml("#2E7D4F");
        }
        ws.Range(rad, 1, rad + 2, 3 + uker.Count).Style.Font.Bold = true;

        ws.Column(1).Width = 40;
        ws.Column(2).Width = 10;
        ws.Column(3).Width = 20;
        for (var i = 0; i < uker.Count; i++)
        {
            ws.Column(4 + i).Width = 8;
            ws.Column(4 + i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.SheetView.FreezeRows(1);
        ws.SheetView.FreezeColumns(3);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
