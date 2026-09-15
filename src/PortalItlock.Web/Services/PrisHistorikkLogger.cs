using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public static class PrisHistorikkLogger
{
    public static void Logg(ApplicationDbContext db, Component entity, decimal? nyNetto, decimal? nyVeil, string kilde)
    {
        if (entity.PrisNetto == nyNetto && entity.PrisVeiledende == nyVeil)
        {
            return;
        }

        // entity.Id er fortsatt 0 om varen er opprettet men ikke lagret ennå
        // (f.eks. en duplikatrad i samme prisimport-fil som traff en helt
        // ny, ennå ulagret vare fra en tidligere rad) - PrisHistorikk.ComponentId
        // ville da blitt satt til 0 (ingen slik vare finnes), som bryter
        // FOREIGN KEY-constrainten ved lagring. Ingen reell prishistorikk å
        // logge for en vare som ikke finnes i databasen ennå uansett.
        if (entity.Id == 0)
        {
            return;
        }

        db.Add(new PrisHistorikk
        {
            ComponentId = entity.Id,
            GammelPrisNetto = entity.PrisNetto,
            NyPrisNetto = nyNetto,
            GammelPrisVeiledende = entity.PrisVeiledende,
            NyPrisVeiledende = nyVeil,
            Dato = DateTime.Now,
            Kilde = kilde
        });
    }
}
