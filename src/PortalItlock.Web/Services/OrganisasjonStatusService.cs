using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Status for alle organisasjoner på én gang, til oversiktene i adminportalen:
// pris per måned, lisenser, moduler, og om e-post, Tripletex, backup og
// faktura er i orden.
public class OrganisasjonStatusService(PlatformDbContext db, AbonnementService abonnement, PlattformOppsettService oppsett, HemmelighetService hemmelig)
{
    public enum Tilstand { Ok, Mangler, Feil, Ikke }

    public sealed record Status(
        Tenant Tenant,
        string? EpostAdresse,
        string? Pakke,
        decimal PerManed,
        Dictionary<LisensType, int> Lisenser,
        List<string> Moduler,
        Tilstand Epost,
        Tilstand Tripletex,
        Tilstand Backup,
        string? BackupTekst,
        string FakturaTekst,
        Tilstand Faktura,
        DateTime? ProveperiodeTil);

    public async Task<List<Status>> AlleAsync()
    {
        await abonnement.SikrePrislisteAsync();
        var inn = await oppsett.HentAsync();
        var tenants = await db.Tenants.AsNoTracking().OrderByDescending(t => t.ErStandard).ThenBy(t => t.Navn).ToListAsync();
        var perManed = await abonnement.MaanedsbelopForAlleAsync();
        var innstillinger = await db.TenantInnstillinger.AsNoTracking().ToDictionaryAsync(i => i.TenantId);
        var aboer = await db.TenantAbonnementer.AsNoTracking().ToDictionaryAsync(a => a.TenantId);
        var pakker = await db.PrisPakker.AsNoTracking().ToDictionaryAsync(p => p.Nokkel, p => p.Navn);
        var lisenser = await db.TenantLisenser.AsNoTracking().ToListAsync();
        var moduler = await db.TenantModuler.AsNoTracking().Where(m => m.Aktiv).ToListAsync();
        var grense = DateTime.Now.AddDays(-14);
        var backuper = await db.TenantBackuper.AsNoTracking()
            .Where(b => !b.ErPlattform && b.TenantId != null && b.Tidspunkt >= grense)
            .Select(b => new { b.TenantId, b.Tidspunkt, b.Feil })
            .ToListAsync();
        var fakturaer = await db.PlattformFakturaer.AsNoTracking()
            .Select(f => new { f.TenantId, f.Status, f.Forfall, f.PeriodeFra })
            .ToListAsync();
        var itlockTripletex = oppsett.ItlockTripletex() is not null;

        return tenants.Select(t =>
        {
            innstillinger.TryGetValue(t.Id, out var i);
            aboer.TryGetValue(t.Id, out var a);

            var tripletex = t.ErStandard
                ? itlockTripletex ? Tilstand.Ok : Tilstand.Mangler
                : string.IsNullOrWhiteSpace(i?.TripletexEmployeeTokenKryptert) ? Tilstand.Mangler : Tilstand.Ok;

            var egne = backuper.Where(b => b.TenantId == t.Id).OrderByDescending(b => b.Tidspunkt).ToList();
            var sisteOk = egne.FirstOrDefault(b => b.Feil == null);
            var backup = egne.FirstOrDefault() is { Feil: not null } ? Tilstand.Feil
                : sisteOk is not null && sisteOk.Tidspunkt > DateTime.Now.AddDays(-2) ? Tilstand.Ok
                : Tilstand.Mangler;
            var backupTekst = sisteOk is null ? "Ingen kopi ennå" : $"Siste {sisteOk.Tidspunkt:dd.MM HH:mm}";

            var epost = string.IsNullOrWhiteSpace(i?.EpostNavn) ? Tilstand.Mangler
                : inn.EpostPerOrganisasjon ? Tilstand.Ok : Tilstand.Mangler;

            var egneFakturaer = fakturaer.Where(f => f.TenantId == t.Id).ToList();
            var forfalt = egneFakturaer.Any(f => f.Status == PlattformFakturaStatus.Fakturert && f.Forfall is { } ff && ff.Date < DateTime.Today);
            var utkast = egneFakturaer.Any(f => f.Status == PlattformFakturaStatus.Utkast);
            var siste = egneFakturaer.OrderByDescending(f => f.PeriodeFra).FirstOrDefault();
            var (fakturaTekst, faktura) = t.ErStandard ? ("Egen", Tilstand.Ikke)
                : a?.ProveperiodeTil is { } p && p.Date >= DateTime.Today ? ("Prøve", Tilstand.Ikke)
                : forfalt ? ("Forfalt", Tilstand.Feil)
                : utkast ? ("Utkast", Tilstand.Mangler)
                : siste is null ? ("Ingen ennå", Tilstand.Ikke)
                : siste.Status == PlattformFakturaStatus.Betalt ? ("Betalt", Tilstand.Ok)
                : ("Sendt", Tilstand.Ok);

            return new Status(
                t,
                i?.EpostNavn is { Length: > 0 } navn ? OrganisasjonOppsett.Adresse(navn) : null,
                a?.Pakke is { } pk && pakker.TryGetValue(pk, out var pakkeNavn) ? pakkeNavn : null,
                t.ErStandard ? 0 : perManed.GetValueOrDefault(t.Id),
                lisenser.Where(l => l.TenantId == t.Id).GroupBy(l => l.Type).ToDictionary(g => g.Key, g => g.Sum(l => l.AntallTildelt)),
                moduler.Where(m => m.TenantId == t.Id).Select(m => TilleggsModuler.Alle.FirstOrDefault(x => x.Nokkel == m.Modul)?.Navn ?? m.Modul).ToList(),
                epost,
                tripletex,
                backup,
                backupTekst,
                fakturaTekst,
                faktura,
                a?.ProveperiodeTil);
        }).ToList();
    }

    public static string Klasse(Tilstand t) => t switch
    {
        Tilstand.Ok => "pa-ok",
        Tilstand.Mangler => "pa-gul",
        Tilstand.Feil => "pa-rod",
        _ => ""
    };
}
