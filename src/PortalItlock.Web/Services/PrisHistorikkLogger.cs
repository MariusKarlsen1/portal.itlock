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
