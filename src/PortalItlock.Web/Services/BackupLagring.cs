using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Hvor backupfilene ligger. Azure Blob Storage brukes når Backup:AzureSasUrl
// er satt (en container-adresse med SAS-token, f.eks. i Norway East), ellers
// en mappe på serveren (Backup:Mappe, eller «backups» ved siden av databasene).
public interface IBackupLagring
{
    BackupLagringType Type { get; }
    string Beskrivelse { get; }
    Task LagreAsync(string sti, string kildefil, CancellationToken ct = default);
    Task HentAsync(string sti, string malfil, CancellationToken ct = default);
    Task SlettAsync(string sti, CancellationToken ct = default);
}

public sealed class LokalBackupLagring(string mappe) : IBackupLagring
{
    public BackupLagringType Type => BackupLagringType.Lokal;
    public string Beskrivelse => "På serveren";

    private string Full(string sti) => Path.Combine(mappe, sti.Replace('/', Path.DirectorySeparatorChar));

    public Task LagreAsync(string sti, string kildefil, CancellationToken ct = default)
    {
        var mal = Full(sti);
        Directory.CreateDirectory(Path.GetDirectoryName(mal)!);
        File.Copy(kildefil, mal, overwrite: true);
        return Task.CompletedTask;
    }

    public Task HentAsync(string sti, string malfil, CancellationToken ct = default)
    {
        File.Copy(Full(sti), malfil, overwrite: true);
        return Task.CompletedTask;
    }

    public Task SlettAsync(string sti, CancellationToken ct = default)
    {
        var fil = Full(sti);
        if (File.Exists(fil))
        {
            File.Delete(fil);
        }

        return Task.CompletedTask;
    }
}

// Azure Blob via REST med SAS-token: ingen egen pakke, og nøkkelen til
// lagringskontoen trengs ikke på serveren.
public sealed class AzureBackupLagring(HttpClient http, string containerSasUrl) : IBackupLagring
{
    public BackupLagringType Type => BackupLagringType.Azure;
    public string Beskrivelse => "Azure Blob Storage";

    private Uri Url(string sti)
    {
        var deler = containerSasUrl.Split('?', 2);
        var sti2 = string.Join('/', sti.Split('/').Select(Uri.EscapeDataString));
        return new Uri($"{deler[0].TrimEnd('/')}/{sti2}{(deler.Length > 1 ? "?" + deler[1] : "")}");
    }

    public async Task LagreAsync(string sti, string kildefil, CancellationToken ct = default)
    {
        await using var fil = File.OpenRead(kildefil);
        using var req = new HttpRequestMessage(HttpMethod.Put, Url(sti)) { Content = new StreamContent(fil) };
        req.Headers.Add("x-ms-blob-type", "BlockBlob");
        req.Headers.Add("x-ms-version", "2021-08-06");
        using var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure svarte {(int)resp.StatusCode} ved opplasting: {await resp.Content.ReadAsStringAsync(ct)}");
        }
    }

    public async Task HentAsync(string sti, string malfil, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, Url(sti));
        req.Headers.Add("x-ms-version", "2021-08-06");
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure svarte {(int)resp.StatusCode} ved nedlasting.");
        }

        await using var ut = File.Create(malfil);
        await resp.Content.CopyToAsync(ut, ct);
    }

    public async Task SlettAsync(string sti, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, Url(sti));
        req.Headers.Add("x-ms-version", "2021-08-06");
        using var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 404)
        {
            throw new InvalidOperationException($"Azure svarte {(int)resp.StatusCode} ved sletting.");
        }
    }
}
