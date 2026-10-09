using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Husker hvilken visning (kanban/liste/kart, liste/rutenett, osv.) brukeren
// sist valgte i hver enkelt modul, slik at den valgte visningen er standard
// neste gang man åpner modulen - til man bytter tilbake (på eksplisitt
// ønske 2026-10-09). Lagres som ett JSON-objekt på Bruker.VisningsPreferanser
// i stedet for én kolonne pr. modul, slik at nye moduler kan ta i bruk dette
// uten noen ny migrasjon. Scoped per krets - leser brukerens lagrede valg én
// gang pr. side-økt, og skriver rett til databasen (uten EF-sporing, se
// ExecuteUpdateAsync) for å unngå å kollidere med andre sider sin egen
// sporede ApplicationDbContext-bruk i samme krets.
public class VisningPreferanseService(AuthenticationStateProvider AuthProvider, ApplicationDbContext Db)
{
    private int? _brukerId;
    private Dictionary<string, string>? _preferanser;
    private bool _forsoktLastet;

    private async Task SikreLastetAsync()
    {
        if (_forsoktLastet)
        {
            return;
        }

        _forsoktLastet = true;
        _preferanser = [];

        var state = await AuthProvider.GetAuthenticationStateAsync();
        var claim = state.User.FindFirst("BrukerId")?.Value;
        if (claim is null || !int.TryParse(claim, out var id))
        {
            return;
        }

        _brukerId = id;

        var json = await Db.Brukere
            .Where(b => b.Id == id)
            .Select(b => b.VisningsPreferanser)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            _preferanser = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            // Ugyldig/korrupt lagret verdi - fortsetter bare med tomme preferanser.
            _preferanser = [];
        }
    }

    public async Task<string> HentAsync(string modul, string standard)
    {
        await SikreLastetAsync();
        return _preferanser!.TryGetValue(modul, out var verdi) && !string.IsNullOrWhiteSpace(verdi)
            ? verdi
            : standard;
    }

    public async Task SettAsync(string modul, string verdi)
    {
        await SikreLastetAsync();

        if (_preferanser!.TryGetValue(modul, out var eksisterende) && eksisterende == verdi)
        {
            return;
        }

        _preferanser[modul] = verdi;

        if (_brukerId is null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(_preferanser);
        await Db.Brukere
            .Where(b => b.Id == _brukerId.Value)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.VisningsPreferanser, json));
    }
}
