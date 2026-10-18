namespace PortalItlock.Web.Data;

// SQLite-funksjoner til bruk i LINQ-spørringer (registrert i ApplicationDbContext).
public static class SqlFn
{
    // typeof(x) leser bare posthodet i SQLite. Bruk SqlFn.TypeOf(x.Data) != "null"
    // i stedet for x.Data != null når man bare trenger å vite OM en fil/et bilde
    // finnes - "IS NOT NULL" på en BLOB laster hele fila inn i minnet for hver rad.
    public static string TypeOf(byte[]? verdi) => throw new NotSupportedException("Kun til bruk i EF Core-spørringer.");
}
