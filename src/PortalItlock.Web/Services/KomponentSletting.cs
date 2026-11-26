using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Sletter en vare. Varen og filene (ComponentFiler) deler tabellen Components,
// så EF sin ExecuteDelete nekter («contains data for multiple entity types»).
// Raden slettes derfor med SQL; koblinger med fremmednøkkel i databasen følger
// sine egne regler (cascade / set null).
public static class KomponentSletting
{
    public static Task<int> SlettAsync(ApplicationDbContext db, int komponentId) =>
        db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Components WHERE Id = {komponentId}");
}
