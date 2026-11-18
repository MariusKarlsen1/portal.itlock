using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Data;

// Component.Filer er en valgfri del av samme tabell (table splitting). Når EF
// Core skal hente den via en join (Include(c => c.Filer) eller c.Filer.X i en
// Select/Where) lager den en LEFT JOIN mot en underspørring over HELE
// Components-tabellen med en CASE-betingelse som ikke kan bruke indeks - altså
// rader × rader sammenligninger, der hver leser filene. Med 25 000 varer hang
// komponentregisteret i minutter, og bare det å åpne én vare leste alle filene
// i databasen. Bruk derfor disse i stedet:
//  - LastFilerAsync: henter filene til én vare som eget oppslag på Id.
//  - KomponenterMedFilAsync: hvilke varer som HAR en fil, uten å lese filene.
public static class KomponentFilerExtensions
{
    private static readonly HashSet<string> FilKolonner = ["FdvData", "MontasjebladData", "DatabladData", "BildeData"];

    public static async Task<Component> LastFilerAsync(this ApplicationDbContext db, Component komponent)
    {
        if (komponent.Filer is null)
        {
            await db.Entry(komponent).Reference(c => c.Filer).LoadAsync();
        }

        FyllFraDisk(db, komponent);
        return komponent;
    }

    // Filene ligger på disk (KomponentFilLager). De legges inn på varen som om
    // de var lest fra databasen - uendret, så de ikke lagres på nytt.
    private static void FyllFraDisk(ApplicationDbContext db, Component komponent)
    {
        if (db.KomponentFilMappe is not { } mappe || komponent.Id <= 0)
        {
            return;
        }

        var fraDisk = KomponentFilLager.Kolonner
            .Select(k => (Kolonne: k, Data: KomponentFilLager.Les(mappe, komponent.Id, k)))
            .Where(x => x.Data is not null)
            .ToList();
        if (fraDisk.Count == 0)
        {
            return;
        }

        var sporet = db.Entry(komponent).State != EntityState.Detached;
        if (komponent.Filer is null)
        {
            var filer = new ComponentFiler { Id = komponent.Id };
            if (sporet)
            {
                db.Entry(filer).State = EntityState.Unchanged;
            }
            komponent.Filer = filer;
        }

        var e = sporet ? db.Entry(komponent.Filer) : null;
        foreach (var (kolonne, data) in fraDisk)
        {
            if (e is not null)
            {
                var prop = e.Property(kolonne);
                if (prop.CurrentValue is not null)
                {
                    continue;
                }
                prop.CurrentValue = data;
                prop.OriginalValue = data;
                prop.IsModified = false;
            }
            else
            {
                typeof(ComponentFiler).GetProperty(kolonne)!.SetValue(komponent.Filer, data);
            }
        }
    }

    public static async Task<Component?> LastFilerAsync(this ApplicationDbContext db, Task<Component?> henting)
    {
        var komponent = await henting;
        return komponent is null ? null : await db.LastFilerAsync(komponent);
    }

    public static Task<HashSet<int>> KomponenterMedFilAsync(this ApplicationDbContext db, string kolonne, IReadOnlyCollection<int>? ider = null)
    {
        if (!FilKolonner.Contains(kolonne))
        {
            throw new ArgumentException($"Ukjent filkolonne «{kolonne}».", nameof(kolonne));
        }

        return MedFilAsync(db, kolonne, ider);
    }

    // I databasen (ikke flyttet ennå) eller på disk.
    private static async Task<HashSet<int>> MedFilAsync(ApplicationDbContext db, string kolonne, IReadOnlyCollection<int>? ider)
    {
        var svar = await db.RaderMedBlobAsync("Components", kolonne, ider);
        if (db.KomponentFilMappe is { } mappe)
        {
            svar.UnionWith(KomponentFilLager.IderMedFil(mappe, kolonne, ider));
        }
        return svar;
    }

    // Id-ene til radene der BLOB-kolonnen har innhold. Tabell/kolonne kommer
    // alltid fra koden (aldri fra brukeren), så de kan settes rett inn i SQL-en.
    internal static async Task<HashSet<int>> RaderMedBlobAsync(this ApplicationDbContext db, string tabell, string kolonne, IReadOnlyCollection<int>? ider)
    {
        if (ider is { Count: 0 })
        {
            return [];
        }

        // typeof() leser bare posthodet - "IS NOT NULL" ville lastet hele fila.
        var sql = $"SELECT \"Id\" AS \"Value\" FROM \"{tabell}\" WHERE typeof(\"{kolonne}\") <> 'null'";
        if (ider is not null)
        {
            sql += $" AND \"Id\" IN ({string.Join(",", ider.Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)))})";
        }

        return (await db.Database.SqlQueryRaw<int>(sql).ToListAsync()).ToHashSet();
    }
}