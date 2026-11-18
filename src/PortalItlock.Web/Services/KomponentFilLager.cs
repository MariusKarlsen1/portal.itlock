using System.Globalization;
using Microsoft.Data.Sqlite;

namespace PortalItlock.Web.Services;

// Filene på varene (bilde, FDV, montasjeblad og datablad) ligger som vanlige
// filer på disken ved siden av databasen, ikke inne i den:
//
//   <mappen til .db-fila>/filer/<databasenavn>/komponenter/<vare-id>/<bilde|fdv|montasjeblad|datablad>
//
// Da blir databasen liten og rask, og en ny kolonne i varetabellen krever ikke
// at alle filene kopieres. ApplicationDbContext skriver og leser filene her
// (se SaveChanges og KomponentFilerExtensions), KomponentFilFlytting flytter
// filer som fortsatt ligger i databasen, og backupen baker dem inn i kopien.
//
// En database i minnet (testene) har ingen mappe - da ligger filene i databasen som før.
public static class KomponentFilLager
{
    public static readonly string[] Kolonner = ["FdvData", "MontasjebladData", "DatabladData", "BildeData"];

    // Hindrer at bakgrunnsflyttingen og en lagring flytter samme fil samtidig.
    public static readonly object Las = new();

    public static string? Mappe(string? tilkobling)
    {
        if (string.IsNullOrWhiteSpace(tilkobling))
        {
            return null;
        }

        try
        {
            var b = new SqliteConnectionStringBuilder(tilkobling);
            var fil = b.DataSource;
            if (string.IsNullOrWhiteSpace(fil) || fil == ":memory:" || b.Mode == SqliteOpenMode.Memory
                || fil.Contains("mode=memory", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var full = Path.GetFullPath(fil);
            return Path.Combine(Path.GetDirectoryName(full)!, "filer", Path.GetFileNameWithoutExtension(full), "komponenter");
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Filnavn(string kolonne) => kolonne switch
    {
        "FdvData" => "fdv",
        "MontasjebladData" => "montasjeblad",
        "DatabladData" => "datablad",
        "BildeData" => "bilde",
        _ => throw new ArgumentException($"Ukjent filkolonne «{kolonne}».", nameof(kolonne))
    };

    private static string VareMappe(string mappe, int id) => Path.Combine(mappe, id.ToString(CultureInfo.InvariantCulture));

    public static string Sti(string mappe, int id, string kolonne) => Path.Combine(VareMappe(mappe, id), Filnavn(kolonne));

    public static byte[]? Les(string mappe, int id, string kolonne)
    {
        var sti = Sti(mappe, id, kolonne);
        return File.Exists(sti) ? File.ReadAllBytes(sti) : null;
    }

    // Skriver fila under et midlertidig navn og kontrollerer at hele fila kom
    // ned. Flytt() gjør den gjeldende - først når databasen er lagret.
    public static string SkrivMidlertidig(string mappe, int id, string kolonne, byte[] data)
    {
        var sti = Sti(mappe, id, kolonne);
        Directory.CreateDirectory(Path.GetDirectoryName(sti)!);
        var tmp = $"{sti}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(tmp, data);
            if (new FileInfo(tmp).Length != data.Length)
            {
                throw new IOException($"Fila for vare {id} ble ikke skrevet helt ned.");
            }
        }
        catch
        {
            SlettStille(tmp);
            throw;
        }
        return tmp;
    }

    public static void Flytt(string tmp, string mappe, int id, string kolonne)
    {
        lock (Las)
        {
            File.Move(tmp, Sti(mappe, id, kolonne), overwrite: true);
        }
    }

    public static void Skriv(string mappe, int id, string kolonne, byte[] data) =>
        Flytt(SkrivMidlertidig(mappe, id, kolonne, data), mappe, id, kolonne);

    public static void Slett(string mappe, int id, string kolonne)
    {
        lock (Las)
        {
            SlettStille(Sti(mappe, id, kolonne));
        }
    }

    public static void SlettVare(string mappe, int id)
    {
        try
        {
            var d = VareMappe(mappe, id);
            if (Directory.Exists(d))
            {
                Directory.Delete(d, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    internal static void SlettStille(string sti)
    {
        try
        {
            if (File.Exists(sti))
            {
                File.Delete(sti);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    // Id-ene til varene som har fila liggende på disken.
    public static HashSet<int> IderMedFil(string mappe, string kolonne, IReadOnlyCollection<int>? ider = null)
    {
        var navn = Filnavn(kolonne);
        if (!Directory.Exists(mappe))
        {
            return [];
        }

        if (ider is not null)
        {
            return ider.Where(i => File.Exists(Path.Combine(VareMappe(mappe, i), navn))).ToHashSet();
        }

        var svar = new HashSet<int>();
        foreach (var d in Directory.EnumerateDirectories(mappe))
        {
            if (int.TryParse(Path.GetFileName(d), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                && File.Exists(Path.Combine(d, navn)))
            {
                svar.Add(id);
            }
        }
        return svar;
    }

    // Kopierer filene til en vare (kopiering mellom organisasjoner).
    public static void KopierVare(string fraMappe, int fraId, string tilMappe, int tilId)
    {
        foreach (var k in Kolonner)
        {
            var fra = Sti(fraMappe, fraId, k);
            if (File.Exists(fra))
            {
                Skriv(tilMappe, tilId, k, File.ReadAllBytes(fra));
            }
        }
    }

    // Legger filene fra disken inn i en kopi av databasen (backup), så kopien
    // er komplett på egen hånd. Gjenopprettes den, flyttes filene ut igjen av
    // KomponentFilFlytting.
    public static async Task<int> BakInnAsync(string dbFil, string? mappe, CancellationToken ct = default)
    {
        if (mappe is null || !Directory.Exists(mappe))
        {
            return 0;
        }

        var antall = 0;
        await using var con = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = dbFil, Pooling = false }.ToString());
        await con.OpenAsync(ct);
        await using var tx = (SqliteTransaction)await con.BeginTransactionAsync(ct);
        foreach (var d in Directory.EnumerateDirectories(mappe))
        {
            if (!int.TryParse(Path.GetFileName(d), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            foreach (var k in Kolonner)
            {
                var sti = Path.Combine(d, Filnavn(k));
                if (!File.Exists(sti))
                {
                    continue;
                }

                await using var cmd = con.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $"UPDATE \"Components\" SET \"{k}\" = $data WHERE \"Id\" = $id";
                cmd.Parameters.AddWithValue("$data", await File.ReadAllBytesAsync(sti, ct));
                cmd.Parameters.AddWithValue("$id", id);
                antall += await cmd.ExecuteNonQueryAsync(ct);
            }
        }
        await tx.CommitAsync(ct);
        return antall;
    }

    // Etter en gjenoppretting: filene på disken hører til dagens data, ikke
    // kopien. Kopien har filene inne i seg, så mappen tømmes og fylles på nytt.
    public static void TomMappe(string? mappe)
    {
        if (mappe is null || !Directory.Exists(mappe))
        {
            return;
        }

        try
        {
            Directory.Delete(mappe, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    // Ledig plass på disken mappen ligger på. Null om det ikke kan leses.
    public static long? LedigPlass(string mappe)
    {
        try
        {
            var d = mappe;
            while (!Directory.Exists(d) && Path.GetDirectoryName(d) is { } forelder && forelder != d)
            {
                d = forelder;
            }
            return new DriveInfo(Path.GetFullPath(d)).AvailableFreeSpace;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
