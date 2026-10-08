using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PortalItlock.Web.Data;

// Registrerer en egen SQL-funksjon på hver SQLite-tilkobling som bruker
// .NET sin fulle Unicode-bevisste OrdinalIgnoreCase-sammenligning (inkl.
// æøå), i stedet for SQLite sin egen innebygde LIKE som kun er
// case-insensitiv for a-z/A-Z. Brukt av ApplicationDbContext.
// InneholderUavhengigAvStorForbokstav (se der), kalt fra LINQ-spørringer som
// f.eks. SokResultat.razor sitt tekstsøk - samme resultat som før, men
// filtrert i databasen i stedet for etter at hele tabellen er hentet til minnet.
public sealed class SqliteUnicodeFunctionsInterceptor : DbConnectionInterceptor
{
    public const string FunksjonNavn = "unicode_contains_ci";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        RegistrerFunksjoner(connection);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        RegistrerFunksjoner(connection);
        await Task.CompletedTask;
    }

    // Public slik at tester som deler en allerede-åpnet in-memory
    // SqliteConnection (EF Core kaller da aldri selv Open() på den, så
    // ConnectionOpened-hendelsen over utløses ikke) kan registrere
    // funksjonen manuelt med nøyaktig samme logikk som i produksjon.
    public static void RegistrerFunksjoner(DbConnection connection)
    {
        if (connection is not SqliteConnection sqlite)
        {
            return;
        }

        sqlite.CreateFunction<string?, string?, bool>(
            FunksjonNavn,
            (tekst, sok) => tekst is not null && sok is not null && tekst.Contains(sok, StringComparison.OrdinalIgnoreCase));
    }
}
