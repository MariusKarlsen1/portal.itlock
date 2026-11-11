using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Befaring og tilbud på serviceavtaler for bygg dere ikke har hatt jobben på.
/// Dørene registreres med komponenttyper (servicetyper) i stedet for nøyaktige
/// produkter: hver type lagres som en plassholder-komponent («Dørautomatikk –
/// ikke spesifisert»). Prisreglene ser bare på typen, så kategori og pris blir
/// riktige med en gang. Produktet velges senere under «Spesifiser komponenter»
/// eller av montøren på første servicerunde.
/// </summary>
public static class ServiceBefaring
{
    public sealed record DorMal(string Navn, string[] Typer);

    /// Ferdige maler som velger flere typer med ett trykk.
    public static readonly DorMal[] Maler =
    [
        new("Kontordør", ["Dørvrider", "Mekanisk lås", "Sylinder"]),
        new("Branndør", ["Dørlukker", "Holdemagnet", "Dørvrider", "Mekanisk lås"]),
        new("Inngang med automatikk", ["Dørautomatikk", "Kortleser", "Elektrisk sluttstykke"]),
        new("Adgangsdør", ["Kortleser", "Elektrisk sluttstykke", "Dørlukker", "Dørvrider"]),
        new("Rømningsdør", ["Panikkbeslag", "Dørlukker"]),
    ];

    public static string PlassholderNavn(string servicetype) => $"{servicetype} – ikke spesifisert";

    /// Sørger for at det finnes én plassholder-komponent per servicetype, og
    /// returnerer servicetype -> komponent. Finnes ingen komponenttype med
    /// servicetypen, lages en.
    public static async Task<Dictionary<string, Component>> SikrePlassholdere(ApplicationDbContext db)
    {
        var typer = await db.ComponentTypes.ToListAsync();
        var plass = await db.Components.Where(c => c.ErPlassholder).ToListAsync();
        var svar = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);
        var endret = false;
        foreach (var st in ServiceRegelMotor.Servicetyper)
        {
            var type = typer.Where(t => string.Equals(ServiceRegelMotor.Servicetype(t), st, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => string.Equals(t.Servicetype, st, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(t => t.Id)
                .FirstOrDefault();
            if (type is null)
            {
                type = new ComponentType { Navn = st, Servicetype = st, Sortering = 900 };
                db.ComponentTypes.Add(type);
                typer.Add(type);
                endret = true;
            }
            var komp = plass.FirstOrDefault(c => c.Navn == PlassholderNavn(st));
            if (komp is null)
            {
                komp = new Component { Navn = PlassholderNavn(st), ErPlassholder = true, Aktiv = false, Type = type };
                db.Components.Add(komp);
                plass.Add(komp);
                endret = true;
            }
            else if (komp.ComponentTypeId != type.Id && type.Id != 0)
            {
                komp.ComponentTypeId = type.Id;
                endret = true;
            }
            svar[st] = komp;
        }
        if (endret)
        {
            await db.SaveChangesAsync();
        }
        return svar;
    }

    /// Neste ledige dørnummer: høyeste tall + 1, med samme prefiks som siste dør.
    public static string NesteDornummer(IEnumerable<string> nummer)
    {
        var liste = nummer.ToList();
        var best = 0;
        var prefiks = "";
        foreach (var n in liste)
        {
            var m = System.Text.RegularExpressions.Regex.Match(n ?? "", @"^(.*?)(\d+)$");
            if (m.Success && int.TryParse(m.Groups[2].Value, out var tall) && tall >= best)
            {
                best = tall;
                prefiks = m.Groups[1].Value;
            }
        }
        return best == 0 ? "1" : $"{prefiks}{best + 1}";
    }

    /// Servicetypene som er registrert som plassholder på døren (ikke spesifisert).
    public static List<string> UspesifiserteTyper(Dor d, IReadOnlyDictionary<int, string?> typeKart) =>
        d.Komponenter.Where(k => k.Component is { ErPlassholder: true })
            .Select(k => k.Component!.ComponentTypeId is { } id ? typeKart.GetValueOrDefault(id) : null)
            .Where(s => s is not null).Select(s => s!).Distinct().ToList();

    /// Produktene som kan velges for en servicetype (aktive, ikke plassholdere).
    public static async Task<List<Component>> ProdukterFor(ApplicationDbContext db, string servicetype, string? sok, int maks = 60)
    {
        var typeKart = await ServiceRegelMotor.HentTypeKart(db);
        var typeIder = typeKart.Where(x => string.Equals(x.Value, servicetype, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToList();
        var q = db.Components.AsNoTracking().Where(c => c.Aktiv && !c.ErPlassholder && c.ComponentTypeId != null && typeIder.Contains(c.ComponentTypeId.Value));
        if (!string.IsNullOrWhiteSpace(sok))
        {
            var s = sok.Trim().ToLower();
            q = q.Where(c => c.Navn.ToLower().Contains(s) || (c.Produktkode != null && c.Produktkode.ToLower().Contains(s)) || (c.Produsent != null && c.Produsent.ToLower().Contains(s)));
        }
        return await q.OrderBy(c => c.Navn).Take(maks).ToListAsync();
    }

    /// Bytter plassholderen for en servicetype på dørene med et valgt produkt.
    /// Returnerer antall komponenter som ble spesifisert.
    public static async Task<int> Spesifiser(ApplicationDbContext db, IEnumerable<int> dorIder, string servicetype, int produktId)
    {
        var ider = dorIder.ToList();
        var produkt = await db.Components.FirstOrDefaultAsync(c => c.Id == produktId && !c.ErPlassholder);
        if (produkt is null || ider.Count == 0) return 0;
        var plassId = await db.Components.Where(c => c.ErPlassholder && c.Navn == PlassholderNavn(servicetype)).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        if (plassId is null) return 0;
        // Nøkkelen er (DorId, ComponentId), så plassholderen fjernes og produktet
        // legges til (eller antallet økes hvis døren allerede har produktet).
        var komp = await db.DorKomponenter.Where(k => ider.Contains(k.DorId) && k.ComponentId == plassId.Value).ToListAsync();
        var finnes = await db.DorKomponenter.Where(k => ider.Contains(k.DorId) && k.ComponentId == produkt.Id).ToListAsync();
        foreach (var k in komp)
        {
            var har = finnes.FirstOrDefault(f => f.DorId == k.DorId);
            if (har is not null)
            {
                har.Antall += k.Antall;
            }
            else
            {
                db.DorKomponenter.Add(new DorKomponent { DorId = k.DorId, ComponentId = produkt.Id, Antall = k.Antall, Enhet = k.Enhet, Plassering = k.Plassering, LevertAv = k.LevertAv, Montert = true });
            }
            db.DorKomponenter.Remove(k);
        }
        db.LoggMerknad = $"Spesifisert: {servicetype} → {produkt.Navn}";
        await db.SaveChangesAsync();
        db.LoggMerknad = null;
        return komp.Count;
    }
}
