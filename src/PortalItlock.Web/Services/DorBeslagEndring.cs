using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Endrer beslagene på en dør rett fra serviceavtalen (E1 «Rett i listen» og
/// E3 «Avkrysning»): legge til en type som «ikke spesifisert», velge eller bytte
/// vare, og fjerne. En dør har én rad per vare (nøkkel DorId + ComponentId), så
/// samme vare to ganger gir høyere antall.
/// </summary>
public static class DorBeslagEndring
{
    public const string Merknad = "Serviceavtale";

    /// Plassholderen («Type – ikke spesifisert») for en komponenttype. Lages hvis den mangler.
    public static async Task<Component> PlassholderFor(ApplicationDbContext db, ComponentType type)
    {
        var komp = await db.Components.FirstOrDefaultAsync(c => c.ErPlassholder && c.ComponentTypeId == type.Id);
        if (komp is not null)
        {
            return komp;
        }
        komp = new Component { Navn = $"{type.Navn} – ikke spesifisert", ErPlassholder = true, Aktiv = false, ComponentTypeId = type.Id };
        db.Components.Add(komp);
        await db.SaveChangesAsync();
        return komp;
    }

    public static async Task LeggTil(ApplicationDbContext db, IEnumerable<int> dorIder, int componentId)
    {
        var ider = dorIder.Distinct().ToList();
        var finnes = await db.DorKomponenter.Where(k => ider.Contains(k.DorId) && k.ComponentId == componentId).ToListAsync();
        foreach (var id in ider)
        {
            if (finnes.FirstOrDefault(k => k.DorId == id) is { } k)
            {
                k.Antall++;
            }
            else
            {
                db.DorKomponenter.Add(new DorKomponent { DorId = id, ComponentId = componentId, Antall = 1, Montert = true });
            }
        }
        await Lagre(db);
    }

    /// Bytter varen på døren (f.eks. fra «ikke spesifisert» til et produkt). Antall og
    /// montering følger med. Har døren allerede den nye varen, slås radene sammen.
    public static async Task Bytt(ApplicationDbContext db, int dorId, int fraComponentId, int tilComponentId)
    {
        if (fraComponentId == tilComponentId)
        {
            return;
        }
        var fra = await db.DorKomponenter.FirstOrDefaultAsync(k => k.DorId == dorId && k.ComponentId == fraComponentId);
        if (fra is null)
        {
            return;
        }
        var til = await db.DorKomponenter.FirstOrDefaultAsync(k => k.DorId == dorId && k.ComponentId == tilComponentId);
        db.DorKomponenter.Remove(fra);
        if (til is not null)
        {
            til.Antall += fra.Antall;
        }
        else
        {
            db.DorKomponenter.Add(new DorKomponent
            {
                DorId = dorId, ComponentId = tilComponentId, Antall = fra.Antall, Enhet = fra.Enhet, Plassering = fra.Plassering,
                LevertAv = fra.LevertAv, Montert = fra.Montert, MontertDato = fra.MontertDato, MontertAvBrukerId = fra.MontertAvBrukerId
            });
        }
        await Lagre(db);
    }

    public static async Task SettAntall(ApplicationDbContext db, int dorId, int componentId, int antall)
    {
        if (await db.DorKomponenter.FirstOrDefaultAsync(k => k.DorId == dorId && k.ComponentId == componentId) is { } k)
        {
            k.Antall = Math.Max(1, antall);
            await Lagre(db);
        }
    }

    public static async Task Fjern(ApplicationDbContext db, int dorId, IEnumerable<int> componentIder)
    {
        var ider = componentIder.ToList();
        var rader = await db.DorKomponenter.Where(k => k.DorId == dorId && ider.Contains(k.ComponentId)).ToListAsync();
        db.DorKomponenter.RemoveRange(rader);
        await Lagre(db);
    }

    private static async Task Lagre(ApplicationDbContext db)
    {
        db.LoggMerknad = Merknad;
        try
        {
            await db.SaveChangesAsync();
        }
        finally
        {
            db.LoggMerknad = null;
        }
    }
}
