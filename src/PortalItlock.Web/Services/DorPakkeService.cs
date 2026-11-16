using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Én pakke per dør: brukes av dørvinduet (DorRedigering) og masseoppdatering
// (DorListe), så reglene er like begge steder.
public static class DorPakkeService
{
    public sealed record PakkeInfo(int Id, string Navn, HashSet<int> KomponentIder, HashSet<int> FunksjonIder);

    public sealed record Resultat(int Fjernet, int IOpsjon, PakkeInfo? Gammel, HashSet<int> Berort);

    public static PakkeInfo Info(Package p) => new(p.Id, p.Navn,
        p.Komponenter.Select(pc => pc.ComponentId).ToHashSet(),
        p.DorFunksjoner.Select(f => f.Id).ToHashSet());

    public static HashSet<int> IdListe(string? s) =>
        (s ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var i) ? i : 0).Where(i => i > 0).ToHashSet();

    // Pakken på døren. Dører som fikk pakke før dette ble lagret, kjennes igjen
    // på at alle varene i pakken står som beslag på døren (største pakke vinner).
    public static PakkeInfo? PakkeFor(Dor dor, IReadOnlyCollection<PakkeInfo> pakker)
    {
        if (dor.PakkeId is int id)
        {
            return pakker.FirstOrDefault(p => p.Id == id);
        }
        var paDoren = dor.Komponenter.Where(k => !k.ErOpsjon).Select(k => k.ComponentId).ToHashSet();
        return pakker
            .Where(p => p.KomponentIder.Count > 0 && p.KomponentIder.IsSubsetOf(paDoren))
            .OrderByDescending(p => p.KomponentIder.Count)
            .FirstOrDefault();
    }

    // Legger pakken på døren: det som kom fra forrige pakke og ikke er i denne
    // fjernes, antall settes lik pakken (aldri dobbelt), montert beholdes. Lagrer ikke.
    // Pakken må være lastet med Komponenter (med Component) og DorFunksjoner.
    public static Resultat Bruk(ApplicationDbContext db, Dor dor, Package pakke, IReadOnlyCollection<PakkeInfo> pakker)
    {
        var gammel = PakkeFor(dor, pakker);
        var gamleKomp = dor.PakkeId is not null ? IdListe(dor.PakkeKomponentIder) : gammel?.KomponentIder ?? [];
        var gamleFunk = dor.PakkeId is not null ? IdListe(dor.PakkeFunksjonIder) : gammel?.FunksjonIder ?? [];
        var nyeKomp = pakke.Komponenter.Select(pk => pk.ComponentId).ToHashSet();
        var nyeFunk = pakke.DorFunksjoner.Select(f => f.Id).ToHashSet();
        var berort = gamleKomp.Union(nyeKomp).ToHashSet();

        var fjernet = 0;
        foreach (var dk in dor.Komponenter.Where(k => !k.ErOpsjon && gamleKomp.Contains(k.ComponentId) && !nyeKomp.Contains(k.ComponentId)).ToList())
        {
            dor.Komponenter.Remove(dk);
            db.DorKomponenter.Remove(dk);
            fjernet++;
        }

        var iOpsjon = 0;
        foreach (var pk in pakke.Komponenter)
        {
            var existing = dor.Komponenter.FirstOrDefault(dk => dk.ComponentId == pk.ComponentId);
            if (existing is { ErOpsjon: true })
            {
                iOpsjon++;
            }
            else if (existing is not null)
            {
                existing.Antall = pk.Antall;
                if (pk.LevertAv is { } lev && existing.LevertAv != lev)
                {
                    existing.LevertAv = lev;
                    if (!existing.MonteresAvOss && existing.Montert)
                    {
                        existing.Montert = false;
                        existing.MontertDato = null;
                        existing.MontertAvBrukerId = null;
                        existing.MontertAvBruker = null;
                    }
                }
            }
            else
            {
                var dk = new DorKomponent { DorId = dor.Id, ComponentId = pk.ComponentId, Component = pk.Component, Antall = pk.Antall, Enhet = pk.Component?.Enhet, LevertAv = pk.LevertAv ?? LevertAv.F };
                dor.Komponenter.Add(dk);
                db.DorKomponenter.Add(dk);
            }
        }

        foreach (var f in dor.Funksjoner.Where(f => gamleFunk.Contains(f.Id) && !nyeFunk.Contains(f.Id)).ToList())
        {
            dor.Funksjoner.Remove(f);
        }
        foreach (var funksjon in pakke.DorFunksjoner)
        {
            if (dor.Funksjoner.All(f => f.Id != funksjon.Id))
            {
                dor.Funksjoner.Add(funksjon);
            }
        }

        dor.PakkeId = pakke.Id;
        dor.PakkeKomponentIder = string.Join(",", nyeKomp.Order());
        dor.PakkeFunksjonIder = string.Join(",", nyeFunk.Order());
        return new Resultat(fjernet, iOpsjon, gammel, berort);
    }
}
