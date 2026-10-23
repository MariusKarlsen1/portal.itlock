using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Lagerstyring fase 4: plukk, montørbiler, forbruk, retur og påfyll.
//
//  - Plukk: reserverte varer flyttes fra hovedlageret til montørens bil eller
//    prosjektlageret (fortsatt bedriftens lagerverdi og fortsatt reservert
//    til jobben), eller føres rett som forbruk.
//  - Forbruk: når en dør merkes ferdig montert (eller varen på døren
//    montert), når en arbeidsordre ferdigmeldes, eller når montøren
//    registrerer «Brukt» i Min bil. SynkForbrukAsync kjøres i bakgrunnen og
//    når lagersidene åpnes, så det skjer uten at noen må huske det.
//  - Påfyll: hver bil kan ha standardbeholdning per vare (VareLagerplass.Minimum).
public class PlukkService(ApplicationDbContext db, LagerService lager)
{
    private static readonly ArbeidsordreStatus[] FerdigeArbeidsordre = [ArbeidsordreStatus.Ferdig, ArbeidsordreStatus.Fakturert];

    // ---------- Plukk ----------

    // Plukker antall per reservasjon. tilLagerId = null betyr «rett til
    // montasje»: varene føres som forbruk med en gang.
    public async Task<int> PlukkAsync(IReadOnlyDictionary<int, int> antallPerReservasjon, int? tilLagerId, int? brukerId, string? brukerNavn)
    {
        var ider = antallPerReservasjon.Where(a => a.Value > 0).Select(a => a.Key).ToList();
        var reservasjoner = await db.LagerReservasjoner.Include(r => r.Prosjekt).Include(r => r.Arbeidsordre)
            .Where(r => ider.Contains(r.Id)).ToListAsync();
        var totalt = 0;

        foreach (var r in reservasjoner)
        {
            var antall = Math.Min(antallPerReservasjon[r.Id], r.Antall - r.Plukket);
            if (antall <= 0)
            {
                continue;
            }

            var tekst = $"Plukk til {r.Prosjekt?.Navn ?? r.Arbeidsordre?.Tittel}";
            if (tilLagerId is { } til && til != r.LagerId)
            {
                await lager.FlyttAsync(r.ComponentId, r.LagerId, til, antall, tekst, brukerId, brukerNavn, r.ProsjektId, r.ArbeidsordreId);
                r.Plukket += antall;
                r.PlukketLagerId = til;
            }
            else
            {
                await lager.RegistrerAsync(r.ComponentId, r.LagerId, -antall, LagerBevegelseType.Forbruk, tekst, brukerId, brukerNavn,
                    prosjektId: r.ProsjektId, arbeidsordreId: r.ArbeidsordreId);
                r.Antall -= antall;
            }

            r.SistEndret = DateTime.Now;
            totalt += antall;
            if (r.Antall == 0 && r.Venter == 0)
            {
                db.LagerReservasjoner.Remove(r);
            }
        }

        await db.SaveChangesAsync();
        return totalt;
    }

    // ---------- Forbruk ----------

    // Trekker antall fra reservasjonen: først fra det som er plukket (bil
    // eller prosjektlager), resten fra hovedlageret.
    private async Task ForbrukFraReservasjonAsync(LagerReservasjon r, int antall, string tekst, int? brukerId, string? brukerNavn)
    {
        antall = Math.Min(antall, r.Antall);
        if (antall <= 0)
        {
            return;
        }

        var fraPlukket = Math.Min(antall, r.Plukket);
        if (fraPlukket > 0)
        {
            await lager.RegistrerAsync(r.ComponentId, r.PlukketLagerId ?? r.LagerId, -fraPlukket, LagerBevegelseType.Forbruk, tekst,
                brukerId, brukerNavn, prosjektId: r.ProsjektId, arbeidsordreId: r.ArbeidsordreId);
        }

        if (antall - fraPlukket > 0)
        {
            await lager.RegistrerAsync(r.ComponentId, r.LagerId, -(antall - fraPlukket), LagerBevegelseType.Forbruk, tekst,
                brukerId, brukerNavn, prosjektId: r.ProsjektId, arbeidsordreId: r.ArbeidsordreId);
        }

        r.Plukket -= fraPlukket;
        r.Antall -= antall;
        r.SistEndret = DateTime.Now;
    }

    // Fører forbruk for det som er montert / brukt siden varen ble reservert.
    // Telles mot det som allerede er ført som forbruk på samme prosjekt eller
    // arbeidsordre, så ingenting trekkes to ganger (heller ikke når montøren
    // har registrert «Brukt» fra bilen først). Returnerer antall stk ført.
    public async Task<int> SynkForbrukAsync()
    {
        var reservasjoner = await db.LagerReservasjoner.Include(r => r.Arbeidsordre).ToListAsync();
        if (reservasjoner.Count == 0)
        {
            return 0;
        }

        var forbrukt = (await db.LagerBevegelser.AsNoTracking()
                .Where(b => b.Type == LagerBevegelseType.Forbruk && (b.ProsjektId != null || b.ArbeidsordreId != null))
                .Select(b => new { b.ProsjektId, b.ArbeidsordreId, b.ComponentId, b.Antall, b.Tidspunkt }).ToListAsync());

        var totalt = 0;
        foreach (var r in reservasjoner)
        {
            int montert;
            string tekst;
            if (r.ProsjektId is { } p)
            {
                var fra = r.Opprettet;
                montert = await db.DorKomponenter.AsNoTracking()
                    .Where(k => k.Dor!.ProsjektId == p && k.ComponentId == r.ComponentId && k.LevertAv == LevertAv.F)
                    .Where(k => (k.Montert && k.MontertDato >= fra) || (k.Dor!.Status == MontasjeStatus.FerdigMontert && k.Dor!.MontertDato >= fra))
                    .SumAsync(k => (int?)k.Antall) ?? 0;
                tekst = "Montert (dør ferdigmeldt)";
            }
            else if (r.Arbeidsordre is { } ao && FerdigeArbeidsordre.Contains(ao.Status))
            {
                // Ferdigmeldt arbeidsordre: alt som er reservert er brukt.
                var brukt = r.Antall;
                await ForbrukFraReservasjonAsync(r, brukt, $"Brukt på {ao.Tittel} (ferdigmeldt)", null, null);
                totalt += brukt;
                r.Venter = 0;
                db.LagerReservasjoner.Remove(r);
                continue;
            }
            else
            {
                var fra = r.Opprettet;
                montert = await db.ArbeidsordreVarer.AsNoTracking()
                    .Where(v => v.ArbeidsordreId == r.ArbeidsordreId && v.ComponentId == r.ComponentId && v.Plukket && v.PlukketDato >= fra)
                    .SumAsync(v => (int?)v.Antall) ?? 0;
                tekst = "Plukket på arbeidsordren";
            }

            var alleredeFort = -forbrukt
                .Where(b => b.ComponentId == r.ComponentId && b.Tidspunkt >= r.Opprettet
                    && (r.ProsjektId is { } pid ? b.ProsjektId == pid : b.ArbeidsordreId == r.ArbeidsordreId))
                .Sum(b => b.Antall);
            var nytt = Math.Min(montert - alleredeFort, r.Antall);
            if (nytt <= 0)
            {
                continue;
            }

            await ForbrukFraReservasjonAsync(r, nytt, tekst, null, null);
            totalt += nytt;
            if (r.Antall == 0 && r.Venter == 0)
            {
                db.LagerReservasjoner.Remove(r);
            }
        }

        await db.SaveChangesAsync();
        return totalt;
    }

    // Montøren registrerer at varer fra bilen er brukt. Er det valgt en
    // arbeidsordre som ikke er ferdig, legges varen også på arbeidsordren
    // (så den blir fakturert).
    public async Task<ArbeidsordreVare?> BruktAsync(int componentId, int bilLagerId, int antall, int? prosjektId, int? arbeidsordreId,
        string? notat, int? brukerId, string? brukerNavn)
    {
        if (antall <= 0)
        {
            return null;
        }

        var vare = await db.Components.FirstAsync(c => c.Id == componentId);
        Arbeidsordre? ao = arbeidsordreId is { } aoId ? await db.Arbeidsordre.FirstOrDefaultAsync(a => a.Id == aoId) : null;
        prosjektId ??= ao?.ProsjektId;

        // Varer som er plukket til denne jobben, trekkes fra reservasjonen.
        var res = await db.LagerReservasjoner
            .Where(r => r.ComponentId == componentId && r.PlukketLagerId == bilLagerId && r.Plukket > 0)
            .Where(r => (arbeidsordreId != null && r.ArbeidsordreId == arbeidsordreId) || (prosjektId != null && r.ProsjektId == prosjektId))
            .FirstOrDefaultAsync();
        var tekst = "Brukt" + (ao is not null ? $" på {ao.Tittel}" : "") + (string.IsNullOrWhiteSpace(notat) ? "" : $" · {notat.Trim()}");

        if (res is not null)
        {
            var fraRes = Math.Min(antall, res.Plukket);
            res.Plukket -= fraRes;
            res.Antall -= fraRes;
            res.SistEndret = DateTime.Now;
            if (res.Antall == 0 && res.Venter == 0)
            {
                db.LagerReservasjoner.Remove(res);
            }
        }

        await lager.RegistrerAsync(componentId, bilLagerId, -antall, LagerBevegelseType.Forbruk, tekst, brukerId, brukerNavn,
            prosjektId: prosjektId, arbeidsordreId: arbeidsordreId);

        ArbeidsordreVare? aoVare = null;
        if (ao is not null && !FerdigeArbeidsordre.Contains(ao.Status))
        {
            aoVare = new ArbeidsordreVare
            {
                ArbeidsordreId = ao.Id,
                ComponentId = componentId,
                Navn = vare.Navn,
                Antall = antall,
                Kostpris = LagerService.KostFor(vare),
                Utpris = vare.PrisVeiledende ?? vare.PrisNetto ?? 0m,
                LagtTilAvBrukerId = brukerId,
                LagtTilDato = DateTime.Now,
                Plukket = true,
                PlukketDato = DateTime.Now,
                PlukketAvBrukerId = brukerId,
                FraLagerId = bilLagerId
            };
            db.ArbeidsordreVarer.Add(aoVare);
        }

        await db.SaveChangesAsync();
        return aoVare;
    }

    // Antallet på en vare som er tatt fra bilen, er endret på arbeidsordren
    // (eller varen er fjernet: tilAntall = 0). Forskjellen føres som forbruk
    // på samme lager og prosjekt, så bilen og prosjektets varekost stemmer.
    public async Task EndreBruktAsync(int arbeidsordreVareId, int fraAntall, int tilAntall, int? brukerId, string? brukerNavn)
    {
        var endring = tilAntall - fraAntall;
        if (endring == 0)
        {
            return;
        }

        var v = await db.ArbeidsordreVarer.Include(x => x.Arbeidsordre).FirstOrDefaultAsync(x => x.Id == arbeidsordreVareId);
        if (v is not { FraLagerId: { } lagerId, ComponentId: { } componentId })
        {
            return;
        }

        var lagerNavn = (await db.Lagre.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lagerId))?.Navn ?? "lageret";
        var tekst = endring > 0
            ? $"Brukt {endring} til på {v.Arbeidsordre?.Tittel}"
            : $"Lagt tilbake i {lagerNavn} fra {v.Arbeidsordre?.Tittel}";
        await lager.RegistrerAsync(componentId, lagerId, -endring, LagerBevegelseType.Forbruk, tekst, brukerId, brukerNavn,
            prosjektId: v.Arbeidsordre?.ProsjektId, arbeidsordreId: v.ArbeidsordreId);
    }

    // Retur fra bil/prosjektlager til et annet lager (normalt hovedlageret).
    // Er varen plukket til en jobb, regnes returen fra den - varen er da
    // fortsatt reservert, men ligger på lageret igjen.
    public async Task ReturAsync(int componentId, int fraLagerId, int tilLagerId, int antall, string? notat, int? brukerId, string? brukerNavn)
    {
        if (antall <= 0 || fraLagerId == tilLagerId)
        {
            return;
        }

        var tekst = "Retur" + (string.IsNullOrWhiteSpace(notat) ? "" : $" · {notat.Trim()}");
        await lager.FlyttAsync(componentId, fraLagerId, tilLagerId, antall, tekst, brukerId, brukerNavn);

        var rest = antall;
        foreach (var r in await db.LagerReservasjoner.Where(r => r.ComponentId == componentId && r.PlukketLagerId == fraLagerId && r.Plukket > 0).ToListAsync())
        {
            if (rest == 0)
            {
                break;
            }

            var n = Math.Min(rest, r.Plukket);
            r.Plukket -= n;
            if (r.LagerId != tilLagerId)
            {
                // Tilbake til et annet lager enn reservasjonens - da er den ikke
                // lenger satt av fysisk, og må reserveres på nytt.
                r.Antall -= n;
                r.Venter += n;
            }

            r.SistEndret = DateTime.Now;
            rest -= n;
        }

        await db.SaveChangesAsync();
    }

    // ---------- Påfyll av bil ----------

    public sealed record Pafyll(Component Vare, int IBil, int TilJobber, int Minimum, int Antall, int LedigHovedlager);

    // Varer der bilens frie beholdning (utenom det som er plukket til jobber)
    // er under standardbeholdningen.
    public async Task<List<Pafyll>> PafyllAsync(int bilLagerId, IReadOnlyDictionary<int, int> ledig)
    {
        var plasser = await db.VareLagerplasser.AsNoTracking().Include(p => p.Component)
            .Where(p => p.LagerId == bilLagerId && p.Minimum != null && p.Minimum > 0).ToListAsync();
        if (plasser.Count == 0)
        {
            return [];
        }

        var beholdning = (await lager.BeholdningAsync(lagerId: bilLagerId)).ToDictionary(b => b.ComponentId, b => b.Antall);
        var tilJobber = (await db.LagerReservasjoner.AsNoTracking().Where(r => r.PlukketLagerId == bilLagerId)
                .Select(r => new { r.ComponentId, r.Plukket }).ToListAsync())
            .GroupBy(r => r.ComponentId).ToDictionary(g => g.Key, g => g.Sum(r => r.Plukket));

        var resultat = new List<Pafyll>();
        foreach (var p in plasser.Where(p => p.Component is { Aktiv: true }))
        {
            var iBil = beholdning.GetValueOrDefault(p.ComponentId);
            var jobb = tilJobber.GetValueOrDefault(p.ComponentId);
            var fri = iBil - jobb;
            if (fri < p.Minimum!.Value)
            {
                resultat.Add(new Pafyll(p.Component!, iBil, jobb, p.Minimum.Value, p.Minimum.Value - fri, ledig.GetValueOrDefault(p.ComponentId)));
            }
        }

        return resultat.OrderBy(r => r.Vare.Navn).ToList();
    }
}
