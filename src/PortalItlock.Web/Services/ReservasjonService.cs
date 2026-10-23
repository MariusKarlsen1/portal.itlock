using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Lagerstyring fase 3: materialbehov og reservasjoner.
//
// Behovet hentes fra det som allerede finnes i portalen:
//  - prosjekt: varene på dørene (Levert av F = vi leverer), ikke montert
//    (verken varen eller hele døren ferdig montert)
//  - arbeidsordre: varene på arbeidsordren, ikke plukket
// «Ledig» er beholdningen på hovedlager(ene) minus det som er reservert.
// Reservasjonen tar det som er ledig, resten blir «venter» og går inn i
// innkjøpsforslaget. Ved varemottak fylles ventende reservasjoner først.
public class ReservasjonService(ApplicationDbContext db, LagerService lager)
{
    // Prosjekter som regnes som bestilt/i arbeid (tilbud som ikke er
    // akseptert, og avsluttede prosjekter, reserverer ikke varer).
    private static readonly ProsjektStatus[] AktiveProsjekt =
        [ProsjektStatus.Aktiv, ProsjektStatus.Registrert, ProsjektStatus.UnderArbeid];

    private static readonly ArbeidsordreStatus[] AapneArbeidsordre =
        [ArbeidsordreStatus.Ny, ArbeidsordreStatus.Klar, ArbeidsordreStatus.Pagaende];

    public sealed record Kilde(int? ProsjektId, int? ArbeidsordreId)
    {
        public string Nokkel => ProsjektId is { } p ? $"p{p}" : $"a{ArbeidsordreId}";
    }

    public sealed class Behovskilde
    {
        public required Kilde Kilde { get; init; }
        public required string Navn { get; init; }
        public string? Undertekst { get; init; }
        public DateTime? Montasje { get; init; }
        public bool Aktiv { get; init; } = true;
        public Dictionary<int, int> Behov { get; init; } = [];
    }

    // ---------- Behov ----------

    public async Task<List<Behovskilde>> AlleBehovAsync()
    {
        var montasje = await MontasjedatoerAsync();

        var dorBehov = await db.DorKomponenter.AsNoTracking()
            .Where(k => k.LevertAv == LevertAv.F && !k.Montert && k.Dor!.Status != MontasjeStatus.FerdigMontert && k.ComponentId != 0 && k.Antall > 0)
            .Where(k => k.Dor!.Prosjekt!.Status == null || AktiveProsjekt.Contains(k.Dor!.Prosjekt!.Status!.Value))
            .GroupBy(k => new { k.Dor!.ProsjektId, k.ComponentId })
            .Select(g => new { g.Key.ProsjektId, g.Key.ComponentId, Antall = g.Sum(k => k.Antall) })
            .ToListAsync();

        var aoBehov = (await db.ArbeidsordreVarer.AsNoTracking()
            .Where(v => v.ComponentId != null && !v.Plukket && v.Antall > 0 && AapneArbeidsordre.Contains(v.Arbeidsordre!.Status))
            .Select(v => new { v.ArbeidsordreId, ComponentId = v.ComponentId!.Value, v.Antall })
            .ToListAsync())
            .Concat((await TilbudBehovAsync(null)).Select(t => new { t.ArbeidsordreId, t.ComponentId, t.Antall }))
            .GroupBy(v => new { v.ArbeidsordreId, v.ComponentId })
            .Select(g => new { g.Key.ArbeidsordreId, g.Key.ComponentId, Antall = g.Sum(v => v.Antall) })
            .ToList();

        // Kilder som har reservasjoner, men ikke lenger behov (avsluttet,
        // montert, fjernet) - de skal med så de kan frigis.
        var medReservasjon = await db.LagerReservasjoner.AsNoTracking()
            .Select(r => new { r.ProsjektId, r.ArbeidsordreId }).Distinct().ToListAsync();

        var prosjektIder = dorBehov.Select(d => d.ProsjektId)
            .Concat(medReservasjon.Where(r => r.ProsjektId != null).Select(r => r.ProsjektId!.Value)).Distinct().ToList();
        var aoIder = aoBehov.Select(a => a.ArbeidsordreId)
            .Concat(medReservasjon.Where(r => r.ArbeidsordreId != null).Select(r => r.ArbeidsordreId!.Value)).Distinct().ToList();

        var prosjekter = await db.Prosjekter.AsNoTracking().Include(p => p.Kunde)
            .Where(p => prosjektIder.Contains(p.Id)).ToListAsync();
        var arbeidsordre = await db.Arbeidsordre.AsNoTracking().Include(a => a.Prosjekt).Include(a => a.Kunde)
            .Where(a => aoIder.Contains(a.Id)).ToListAsync();

        var resultat = new List<Behovskilde>();
        foreach (var p in prosjekter)
        {
            resultat.Add(new Behovskilde
            {
                Kilde = new Kilde(p.Id, null),
                Navn = p.Navn,
                Undertekst = p.Kunde?.Navn,
                Montasje = montasje.TryGetValue(p.Id, out var m) ? m : null,
                Aktiv = p.Status is null || AktiveProsjekt.Contains(p.Status.Value),
                Behov = dorBehov.Where(d => d.ProsjektId == p.Id).ToDictionary(d => d.ComponentId, d => d.Antall)
            });
        }

        foreach (var a in arbeidsordre)
        {
            resultat.Add(new Behovskilde
            {
                Kilde = new Kilde(null, a.Id),
                Navn = a.Tittel,
                Undertekst = "Arbeidsordre" + (a.Prosjekt is { } p ? $" · {p.Navn}" : a.Kunde is { } k ? $" · {k.Navn}" : ""),
                Montasje = a.PlanlagtDato,
                Aktiv = AapneArbeidsordre.Contains(a.Status),
                Behov = aoBehov.Where(v => v.ArbeidsordreId == a.Id).ToDictionary(v => v.ComponentId, v => v.Antall)
            });
        }

        return resultat;
    }

    // Første planlagte arbeidsordre (som ikke er ferdig) per prosjekt.
    private async Task<Dictionary<int, DateTime>> MontasjedatoerAsync()
    {
        var ao = await db.Arbeidsordre.AsNoTracking()
            .Where(a => a.ProsjektId != null && a.PlanlagtDato != null && AapneArbeidsordre.Contains(a.Status))
            .Select(a => new { ProsjektId = a.ProsjektId!.Value, Dato = a.PlanlagtDato!.Value })
            .ToListAsync();
        return ao.GroupBy(a => a.ProsjektId).ToDictionary(g => g.Key, g => g.Min(a => a.Dato));
    }


    // ---------- Arbeidsordre ----------

    // Varene fra tilbudet på åpne arbeidsordre som ikke er plukket. Har
    // prosjektet dører med varer, reserveres de via prosjektet, så da telles
    // ikke tilbudet med (ellers ble varene satt av to ganger).
    private async Task<List<(int ArbeidsordreId, int ComponentId, int Antall)>> TilbudBehovAsync(int? arbeidsordreId)
    {
        var ordre = await db.Arbeidsordre.AsNoTracking()
            .Where(a => a.TilbudId != null && AapneArbeidsordre.Contains(a.Status))
            .Where(a => arbeidsordreId == null || a.Id == arbeidsordreId)
            .Where(a => a.ProsjektId == null || !db.DorKomponenter.Any(k => k.Dor!.ProsjektId == a.ProsjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0))
            .Select(a => new { a.Id, TilbudId = a.TilbudId!.Value })
            .ToListAsync();
        if (ordre.Count == 0)
        {
            return [];
        }

        var tilbudIder = ordre.Select(o => o.TilbudId).Distinct().ToList();
        var linjer = await db.TilbudLinjer.AsNoTracking()
            .Where(l => tilbudIder.Contains(l.TilbudId) && l.ComponentId != null && !l.ErGruppering && !l.Plukket && l.Antall > 0 && l.LevertAv == LevertAv.F)
            .Select(l => new { l.TilbudId, ComponentId = l.ComponentId!.Value, l.Antall })
            .ToListAsync();

        return ordre
            .SelectMany(o => linjer.Where(l => l.TilbudId == o.TilbudId).Select(l => (o.Id, l.ComponentId, l.Antall)))
            .GroupBy(x => (x.Id, x.ComponentId))
            .Select(g => (g.Key.Id, g.Key.ComponentId, g.Sum(x => x.Antall)))
            .ToList();
    }

    // Det arbeidsordren trenger fra lageret: varer på ordren og fra tilbudet
    // som ikke er plukket ennå.
    public async Task<Dictionary<int, int>> ArbeidsordreBehovAsync(int arbeidsordreId)
    {
        var status = await db.Arbeidsordre.AsNoTracking().Where(a => a.Id == arbeidsordreId).Select(a => (ArbeidsordreStatus?)a.Status).FirstOrDefaultAsync();
        if (status is null || !AapneArbeidsordre.Contains(status.Value))
        {
            return [];
        }

        var varer = await db.ArbeidsordreVarer.AsNoTracking()
            .Where(v => v.ArbeidsordreId == arbeidsordreId && v.ComponentId != null && !v.Plukket && v.Antall > 0)
            .Select(v => new { ComponentId = v.ComponentId!.Value, v.Antall })
            .ToListAsync();

        return varer.Select(v => (v.ComponentId, v.Antall))
            .Concat((await TilbudBehovAsync(arbeidsordreId)).Select(t => (t.ComponentId, t.Antall)))
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Item2));
    }

    // Setter av det arbeidsordren trenger: det som er ledig på hovedlageret
    // reserveres, resten venter (og kommer med i innkjøpsforslaget). Frigir
    // det som ikke trengs lenger (tilbudet koblet fra, vare fjernet).
    // Ingenting bestilles herfra. Ferdigmeldte ordre røres ikke: der fører
    // PlukkService.SynkForbrukAsync det som er satt av, som forbruk.
    public async Task<Resultat?> SikreArbeidsordreAsync(int arbeidsordreId, string? brukerNavn)
    {
        var status = await db.Arbeidsordre.AsNoTracking().Where(a => a.Id == arbeidsordreId).Select(a => (ArbeidsordreStatus?)a.Status).FirstOrDefaultAsync();
        if (status is null || !AapneArbeidsordre.Contains(status.Value))
        {
            return null;
        }

        return await ReserverAsync(new Kilde(null, arbeidsordreId), await ArbeidsordreBehovAsync(arbeidsordreId), brukerNavn);
    }

    // ---------- Beholdning ----------

    private async Task<List<int>> HovedlagerIderAsync()
    {
        var ider = await db.Lagre.Where(l => l.Aktiv && l.Type == LagerType.Hovedlager).OrderBy(l => l.Id).Select(l => l.Id).ToListAsync();
        if (ider.Count == 0)
        {
            ider.Add((await lager.HovedlagerAsync()).Id);
        }

        return ider;
    }

    // Ledig = på hovedlager minus det som er reservert og ikke plukket ut ennå.
    public async Task<Dictionary<int, int>> LedigAsync()
    {
        var hoved = await HovedlagerIderAsync();
        var paLager = await db.LagerBevegelser.AsNoTracking().Where(b => hoved.Contains(b.LagerId))
            .GroupBy(b => b.ComponentId).Select(g => new { g.Key, Antall = g.Sum(b => b.Antall) }).ToListAsync();
        var reservert = (await db.LagerReservasjoner.AsNoTracking().Select(r => new { r.ComponentId, Rest = r.Antall - r.Plukket }).ToListAsync())
            .GroupBy(r => r.ComponentId).ToDictionary(g => g.Key, g => g.Sum(r => r.Rest));
        var ider = paLager.Select(p => p.Key).Union(reservert.Keys);
        return ider.ToDictionary(id => id, id => (paLager.FirstOrDefault(p => p.Key == id)?.Antall ?? 0) - reservert.GetValueOrDefault(id));
    }

    public async Task<Dictionary<int, int>> ReservertAsync() =>
        (await db.LagerReservasjoner.AsNoTracking().Select(r => new { r.ComponentId, r.Antall }).ToListAsync())
            .GroupBy(r => r.ComponentId).ToDictionary(g => g.Key, g => g.Sum(r => r.Antall));

    public async Task<Dictionary<int, int>> VenterAsync() =>
        (await db.LagerReservasjoner.AsNoTracking().Where(r => r.Venter > 0).Select(r => new { r.ComponentId, r.Venter }).ToListAsync())
            .GroupBy(r => r.ComponentId).ToDictionary(g => g.Key, g => g.Sum(r => r.Venter));

    // ---------- Reservere og frigi ----------

    public sealed record Resultat(int Reservert, int Venter, int Frigitt);

    // «Kontroller lager og reserver»: tilpasser reservasjonene til behovet.
    // Mer behov → ta fra ledig, resten venter. Mindre behov → frigi
    // (først det som venter, så det som er satt av).
    public async Task<Resultat> ReserverAsync(Kilde kilde, IReadOnlyDictionary<int, int> behov, string? brukerNavn)
    {
        var hoved = (await HovedlagerIderAsync())[0];
        var ledig = await LedigAsync();
        var eksisterende = await Reservasjoner(kilde).ToListAsync();
        int nyReservert = 0, nyVenter = 0, frigitt = 0;

        foreach (var componentId in behov.Keys.Union(eksisterende.Select(r => r.ComponentId)).ToList())
        {
            var maal = Math.Max(0, behov.GetValueOrDefault(componentId));
            var res = eksisterende.FirstOrDefault(r => r.ComponentId == componentId);
            if (res is null)
            {
                if (maal == 0)
                {
                    continue;
                }

                res = new LagerReservasjon { ComponentId = componentId, LagerId = hoved, ProsjektId = kilde.ProsjektId, ArbeidsordreId = kilde.ArbeidsordreId, OpprettetAv = brukerNavn };
                db.LagerReservasjoner.Add(res);
            }

            var dekket = res.Antall + res.Venter;
            if (maal > dekket)
            {
                var mangler = maal - dekket;
                var ta = Math.Min(mangler, Math.Max(0, ledig.GetValueOrDefault(componentId)));
                res.Antall += ta;
                res.Venter += mangler - ta;
                ledig[componentId] = ledig.GetValueOrDefault(componentId) - ta;
                nyReservert += ta;
                nyVenter += mangler - ta;
            }
            else if (maal < dekket)
            {
                var overskudd = dekket - maal;
                var fraVenter = Math.Min(overskudd, res.Venter);
                res.Venter -= fraVenter;
                res.Antall -= overskudd - fraVenter;
                frigitt += overskudd;
                res.Plukket = Math.Min(res.Plukket, res.Antall);
            }

            // Har vi fått ledige varer siden sist, flyttes ventende over.
            if (res.Venter > 0 && ledig.GetValueOrDefault(componentId) > 0)
            {
                var flytt = Math.Min(res.Venter, ledig[componentId]);
                res.Venter -= flytt;
                res.Antall += flytt;
                ledig[componentId] -= flytt;
                nyReservert += flytt;
            }

            res.SistEndret = DateTime.Now;
            if (res.Antall == 0 && res.Venter == 0)
            {
                db.LagerReservasjoner.Remove(res);
            }
        }

        await db.SaveChangesAsync();
        return new Resultat(nyReservert, nyVenter, frigitt);
    }

    public async Task<int> FrigiAsync(Kilde kilde)
    {
        var rader = await Reservasjoner(kilde).ToListAsync();
        var antall = rader.Sum(r => r.Antall + r.Venter);
        db.LagerReservasjoner.RemoveRange(rader);
        await db.SaveChangesAsync();
        return antall;
    }

    public IQueryable<LagerReservasjon> Reservasjoner(Kilde kilde) => kilde.ProsjektId is { } p
        ? db.LagerReservasjoner.Where(r => r.ProsjektId == p)
        : db.LagerReservasjoner.Where(r => r.ArbeidsordreId == kilde.ArbeidsordreId);

    // Varemottak: mottatte varer går først til reservasjoner som venter -
    // prosjektet innkjøpet er merket med først, deretter den med tidligst
    // montasje, deretter eldste reservasjon. Returnerer hvem som fikk.
    public async Task<List<(LagerReservasjon Reservasjon, int Antall)>> FordelMottakAsync(int componentId, int antall, int? prosjektId)
    {
        var fordelt = new List<(LagerReservasjon, int)>();
        if (antall <= 0)
        {
            return fordelt;
        }

        var ventende = await db.LagerReservasjoner.Include(r => r.Prosjekt).Include(r => r.Arbeidsordre)
            .Where(r => r.ComponentId == componentId && r.Venter > 0).ToListAsync();
        if (ventende.Count == 0)
        {
            return fordelt;
        }

        var montasje = await MontasjedatoerAsync();
        DateTime Dato(LagerReservasjon r) =>
            (r.ProsjektId is { } p ? montasje.GetValueOrDefault(p) : r.Arbeidsordre?.PlanlagtDato) is { } d && d != default ? d : DateTime.MaxValue;

        foreach (var r in ventende
            .OrderByDescending(r => prosjektId is not null && r.ProsjektId == prosjektId)
            .ThenBy(Dato).ThenBy(r => r.Opprettet))
        {
            if (antall == 0)
            {
                break;
            }

            var ta = Math.Min(antall, r.Venter);
            r.Venter -= ta;
            r.Antall += ta;
            r.SistEndret = DateTime.Now;
            antall -= ta;
            fordelt.Add((r, ta));
        }

        await db.SaveChangesAsync();
        return fordelt;
    }
}
