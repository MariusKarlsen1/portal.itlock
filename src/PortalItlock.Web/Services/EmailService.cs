using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Sender e-post via Resend. Nøkkelen og standardavsenderen settes under
// Innstillinger i adminportalen (serveroppsettet brukes hvis de mangler der).
//
// Når «E-post per organisasjon» er slått på, sendes e-post fra organisasjonen
// fra dens egen adresse, f.eks. «Sørlandet Lås AS <sorlandet-las@full-kontroll.no>»,
// og svar går dit organisasjonen har valgt.
public class EmailService(HttpClient http, PlattformOppsettService oppsett, ITenantContext tenantContext, PlatformDbContext platformDb)
{
    public async Task<bool> SendAsync(string til, string emne, string html, IEnumerable<(string Filnavn, byte[] Data)>? vedlegg = null, string? svarTil = null, int? fraOrganisasjonId = null)
    {
        var apiKey = await oppsett.ResendNokkelAsync();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(til))
        {
            return false;
        }

        var (fraAdresse, svar) = await AvsenderAsync(fraOrganisasjonId);

        var payload = new Dictionary<string, object?>
        {
            ["from"] = fraAdresse,
            ["to"] = new[] { til },
            ["subject"] = emne,
            ["html"] = html
        };

        var replyTo = !string.IsNullOrWhiteSpace(svarTil) ? svarTil : svar;
        if (!string.IsNullOrWhiteSpace(replyTo))
        {
            payload["reply_to"] = replyTo;
        }

        if (vedlegg is not null)
        {
            var vedleggListe = vedlegg
                .Select(v => new { filename = v.Filnavn, content = Convert.ToBase64String(v.Data) })
                .ToList();

            if (vedleggListe.Count > 0)
            {
                payload["attachments"] = vedleggListe;
            }
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Avsender og svaradresse for organisasjonen, eller standardavsenderen.
    public async Task<(string Fra, string? SvarTil)> AvsenderAsync(int? organisasjonId = null)
    {
        var standard = await oppsett.StandardAvsenderAsync();
        var tenantId = organisasjonId ?? tenantContext.Current?.Id;
        if (tenantId is null)
        {
            return (standard, null);
        }

        var org = await platformDb.TenantInnstillinger.AsNoTracking().FirstOrDefaultAsync(i => i.TenantId == tenantId);
        if (org is null || string.IsNullOrWhiteSpace(org.EpostNavn))
        {
            return (standard, null);
        }

        var adresse = OrganisasjonOppsett.Adresse(org.EpostNavn);
        var navn = (org.AvsenderNavn ?? "").Replace("\"", "").Replace("<", "").Replace(">", "").Trim();
        var fra = navn.Length > 0 ? $"\"{navn}\" <{adresse}>" : adresse;
        var svar = org.SvarTilPortal ? adresse : org.SvarTilEpost;
        return (fra, svar);
    }
}
