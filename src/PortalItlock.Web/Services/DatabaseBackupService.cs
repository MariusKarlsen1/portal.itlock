using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Sikkerhetskopierer SQLite-databasen til en egen GitHub-branch (db-backups) via
// GitHub sitt Contents API, siden Railway sine native volum-backups krever Pro-plan.
// Bruker GitHub som lagringssted i stedet for e-post, siden Resend har en grense på
// 40 MB (etter base64-koding) som databasen allerede er for stor til å holde seg under.
public class DatabaseBackupService(ApplicationDbContext db, HttpClient http, IConfiguration config, ILogger<DatabaseBackupService> logger)
{
    private const string Owner = "MariusKarlsen1";
    private const string Repo = "portal.itlock";
    private const string Branch = "db-backups";
    private const string FilPath = "backups/portalitlock.db.gz";

    public async Task<DatabaseBackup> KjorBackupAsync()
    {
        var resultat = new DatabaseBackup { Tidspunkt = DateTime.Now };

        try
        {
            var token = config["GitHub:BackupToken"];
            if (string.IsNullOrWhiteSpace(token))
            {
                resultat.Vellykket = false;
                resultat.Melding = "Ingen GitHub-token konfigurert (GitHub:BackupToken).";
                return await LagreOgReturnerAsync(resultat);
            }

            var connectionString = db.Database.GetConnectionString();
            var dbPath = new SqliteConnectionStringBuilder(connectionString).DataSource;

            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                resultat.Vellykket = false;
                resultat.Melding = $"Fant ikke databasefilen ({dbPath}).";
                return await LagreOgReturnerAsync(resultat);
            }

            // Kopier filen først, for å unngå å lese den midt i en skriveoperasjon.
            var tempCopy = Path.Combine(Path.GetTempPath(), $"backup-{Guid.NewGuid():N}.db");
            byte[] raw;
            File.Copy(dbPath, tempCopy, overwrite: true);
            try
            {
                raw = await File.ReadAllBytesAsync(tempCopy);
            }
            finally
            {
                File.Delete(tempCopy);
            }

            using var komprimertStream = new MemoryStream();
            await using (var gzip = new GZipStream(komprimertStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                await gzip.WriteAsync(raw);
            }
            var komprimert = komprimertStream.ToArray();

            var eksisterendeSha = await HentEksisterendeShaAsync(token);

            var payload = new Dictionary<string, object?>
            {
                ["message"] = $"Automatisk backup {DateTime.Now:dd.MM.yyyy HH:mm}",
                ["content"] = Convert.ToBase64String(komprimert),
                ["branch"] = Branch
            };
            if (eksisterendeSha is not null)
            {
                payload["sha"] = eksisterendeSha;
            }

            using var putRequest = LagRequest(HttpMethod.Put, $"repos/{Owner}/{Repo}/contents/{FilPath}", token);
            putRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var putResponse = await http.SendAsync(putRequest);

            if (!putResponse.IsSuccessStatusCode)
            {
                var feilInnhold = await putResponse.Content.ReadAsStringAsync();
                resultat.Vellykket = false;
                resultat.Melding = $"GitHub svarte {(int)putResponse.StatusCode}: {feilInnhold[..Math.Min(300, feilInnhold.Length)]}";
                logger.LogWarning("Database-backup feilet: {Melding}", resultat.Melding);
            }
            else
            {
                resultat.Vellykket = true;
                resultat.StorrelseBytes = komprimert.LongLength;
                resultat.Melding = $"Lastet opp {komprimert.LongLength / 1024.0 / 1024.0:N1} MB (komprimert fra {raw.LongLength / 1024.0 / 1024.0:N1} MB).";
            }
        }
        catch (Exception ex)
        {
            resultat.Vellykket = false;
            resultat.Melding = $"Feil under backup: {ex.Message}";
            logger.LogError(ex, "Feil under database-backup.");
        }

        return await LagreOgReturnerAsync(resultat);
    }

    private async Task<string?> HentEksisterendeShaAsync(string token)
    {
        using var getRequest = LagRequest(HttpMethod.Get, $"repos/{Owner}/{Repo}/contents/{FilPath}?ref={Branch}", token);
        using var getResponse = await http.SendAsync(getRequest);
        if (!getResponse.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("sha", out var shaEl) ? shaEl.GetString() : null;
    }

    private static HttpRequestMessage LagRequest(HttpMethod metode, string url, string token)
    {
        var request = new HttpRequestMessage(metode, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("portal-itlock-backup", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return request;
    }

    private async Task<DatabaseBackup> LagreOgReturnerAsync(DatabaseBackup resultat)
    {
        db.DatabaseBackuper.Add(resultat);
        await db.SaveChangesAsync();
        return resultat;
    }
}
