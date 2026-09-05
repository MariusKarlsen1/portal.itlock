using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Holder tilbudets varelinje for en sylinder i synk når komponenten legges til på en dør,
/// slik at "Låsplan"-krysset (Component.ErSylinder) automatisk følger med til tilbudet
/// uten at man må trykke "Synk varer" manuelt.
/// </summary>
public class TilbudSyncService(ApplicationDbContext db)
{
    public async Task SyncSylinderAsync(int prosjektId, int componentId)
    {
        var component = await db.Components.FindAsync(componentId);
        if (component is null || !component.ErSylinder)
        {
            return;
        }

        var tilbudListe = await db.Tilbud.Include(t => t.Linjer).Where(t => t.ProsjektId == prosjektId).ToListAsync();
        if (tilbudListe.Count == 0)
        {
            return;
        }

        var dorKomponenter = await db.DorKomponenter
            .Where(dk => dk.Dor!.ProsjektId == prosjektId && dk.ComponentId == componentId)
            .ToListAsync();

        if (dorKomponenter.Count == 0)
        {
            return;
        }

        var antall = dorKomponenter.Sum(dk => dk.Antall);
        var levertAv = dorKomponenter[0].LevertAv;
        var innpris = component.PrisNetto ?? 0;

        foreach (var tilbud in tilbudListe)
        {
            var linje = tilbud.Linjer.FirstOrDefault(l => l.ComponentId == componentId);
            if (linje is not null)
            {
                // Linjen finnes allerede på dette tilbudet (f.eks. fra "Synk varer") - hold antallet oppdatert.
                linje.Antall = antall;
            }
            else if (tilbudListe.Count == 1)
            {
                // Kun trygt å opprette en ny linje automatisk når det ikke er tvetydig hvilket tilbud den hører til.
                db.TilbudLinjer.Add(new TilbudLinje
                {
                    TilbudId = tilbud.Id,
                    ComponentId = componentId,
                    Navn = component.Navn,
                    Innpris = innpris,
                    Antall = antall,
                    Enhet = component.Enhet,
                    MontasjeMinutter = component.MontasjeMinutterProsjekt,
                    Utpris = BeregnUtpris(innpris, tilbud.PrisType, tilbud.Prosentsats),
                    LevertAv = levertAv,
                    Rekkefolge = tilbud.Linjer.Count
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static decimal BeregnUtpris(decimal innpris, TilbudPrisType prisType, decimal prosentsats) => prisType switch
    {
        TilbudPrisType.Paslag => Math.Round(innpris * (1 + prosentsats / 100m), 2),
        _ => prosentsats >= 100 ? innpris : Math.Round(innpris / (1 - prosentsats / 100m), 2)
    };
}
