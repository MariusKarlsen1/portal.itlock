using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Models;

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

        return komponent;
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

        return db.RaderMedBlobAsync("Components", kolonne, ider);
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