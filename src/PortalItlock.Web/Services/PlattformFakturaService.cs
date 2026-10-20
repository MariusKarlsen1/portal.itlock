using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Fakturaene til organisasjonene sendes til itlock sin egen Tripletex som en
// ordre «klar til fakturering», på samme måte som arbeidsordrene i portalen.
// Der fakturerer du med ett klikk, og Tripletex står for fakturanummer, KID,
// mva, EHF, purring og bokføring. Betalingsstatus hentes tilbake hit.
public class PlattformFakturaService(
    PlatformDbContext db,
    TripletexNokkelOppslag tripletexOppslag,
    ILogger<PlattformFakturaService> logger)
{
    // Tripletex for standard-organisasjonen (itlock), uansett hvem som spør.
    private async Task<TripletexService?> ItlockTripletexAsync()
    {
        var itlock = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.ErStandard);
        if (itlock is null)
        {
            return null;
        }

        var tx = tripletexOppslag.Lag(itlock);
        return tx.ErKonfigurert ? tx : null;
    }

    public async Task<bool> TripletexKlarAsync() => await ItlockTripletexAsync() is not null;

    public async Task<string?> SendTilTripletexAsync(int fakturaId, string? av)
    {
        var faktura = await db.PlattformFakturaer.Include(f => f.Linjer).Include(f => f.Tenant).FirstOrDefaultAsync(f => f.Id == fakturaId);
        if (faktura?.Tenant is not { } tenant)
        {
            return "Fant ikke fakturaen.";
        }

        if (faktura.Status != PlattformFakturaStatus.Utkast)
        {
            return "Fakturaen er allerede sendt.";
        }

        var tx = await ItlockTripletexAsync();
        if (tx is null)
        {
            return "Tripletex er ikke satt opp for itlock. Marker fakturaen som sendt hvis du fakturerer på annen måte.";
        }

        var abo = await db.TenantAbonnementer.FirstAsync(a => a.TenantId == tenant.Id);

        // Kunden i Tripletex: lagret fra før, funnet på org.nr., eller ny.
        if (abo.TripletexKundeId is null)
        {
            var (treff, sokFeil) = await tx.SokKunderAsync(tenant.Navn);
            var orgNr = Siffer(tenant.OrgNr);
            var kunde = treff.FirstOrDefault(k => orgNr.Length > 0 && Siffer(k.OrganizationNumber) == orgNr)
                ?? (orgNr.Length == 0 ? treff.FirstOrDefault(k => string.Equals(k.Navn, tenant.Navn, StringComparison.OrdinalIgnoreCase)) : null);

            if (kunde is not null)
            {
                abo.TripletexKundeId = kunde.Id;
            }
            else
            {
                var (nyId, _, opprettFeil) = await tx.OpprettKundeAsync(new TripletexService.KundeOppdatering(
                    tenant.Navn, tenant.OrgNr, abo.FakturaEpost ?? tenant.Epost, tenant.Telefon, tenant.Adresse, tenant.Postnr, tenant.Sted));
                if (nyId is null)
                {
                    faktura.Feil = opprettFeil ?? sokFeil ?? "Klarte ikke å opprette kunden i Tripletex.";
                    await db.SaveChangesAsync();
                    return faktura.Feil;
                }

                abo.TripletexKundeId = nyId;
            }

            await db.SaveChangesAsync();
        }

        var linjer = faktura.Linjer.OrderBy(l => l.Rekkefolge)
            .Select(l => new TripletexService.OrdreLinjeInput(l.Tekst, l.Antall, l.Pris))
            .ToList();
        var referanse = $"Full Kontroll {AbonnementService.PeriodeTekst(faktura.PeriodeFra, faktura.PeriodeTil)}";
        var (ordreId, ordreNr, feil) = await tx.OpprettOrdreAsync(abo.TripletexKundeId!.Value, referanse, linjer);

        faktura.TripletexOrdreId = ordreId;
        faktura.TripletexOrdreNummer = ordreNr;
        faktura.Feil = feil;
        if (ordreId is not null)
        {
            faktura.Status = PlattformFakturaStatus.SendtTilTripletex;
            db.TenantHendelser.Add(new TenantHendelse { TenantId = tenant.Id, Tekst = $"Faktura for {referanse[14..]} sendt til Tripletex (ordre {ordreNr})", Ikon = "upload", Av = av });
        }

        await db.SaveChangesAsync();
        return feil;
    }

    // Når du ikke bruker Tripletex for en faktura.
    public async Task MarkerAsync(int fakturaId, PlattformFakturaStatus status, string? av)
    {
        var faktura = await db.PlattformFakturaer.FirstAsync(f => f.Id == fakturaId);
        faktura.Status = status;
        faktura.Feil = null;
        if (status == PlattformFakturaStatus.Betalt)
        {
            faktura.BetaltDato ??= DateTime.Today;
            faktura.Utestaende = 0;
        }
        else if (status == PlattformFakturaStatus.Fakturert)
        {
            var abo = await db.TenantAbonnementer.AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == faktura.TenantId);
            faktura.Fakturadato ??= DateTime.Today;
            faktura.Forfall ??= DateTime.Today.AddDays(abo?.BetalingsfristDager ?? 14);
            faktura.Utestaende ??= faktura.BelopEksMva * 1.25m;
        }

        var navn = status switch
        {
            PlattformFakturaStatus.Betalt => "betalt",
            PlattformFakturaStatus.Fakturert => "fakturert",
            PlattformFakturaStatus.Kreditert => "kreditert",
            _ => status.ToString().ToLowerInvariant()
        };
        db.TenantHendelser.Add(new TenantHendelse { TenantId = faktura.TenantId, Tekst = $"Faktura for {AbonnementService.PeriodeTekst(faktura.PeriodeFra, faktura.PeriodeTil)} markert som {navn}", Ikon = "receipt", Av = av });
        await db.SaveChangesAsync();
    }

    public async Task SlettUtkastAsync(int fakturaId, string? av)
    {
        var faktura = await db.PlattformFakturaer.FirstOrDefaultAsync(f => f.Id == fakturaId && f.Status == PlattformFakturaStatus.Utkast);
        if (faktura is null)
        {
            return;
        }

        // Perioden kan faktureres på nytt.
        var abo = await db.TenantAbonnementer.FirstOrDefaultAsync(a => a.TenantId == faktura.TenantId);
        if (abo is not null && abo.NesteFakturaFra == faktura.PeriodeTil.AddDays(1))
        {
            abo.NesteFakturaFra = faktura.PeriodeFra;
        }

        db.PlattformFakturaer.Remove(faktura);
        db.TenantHendelser.Add(new TenantHendelse { TenantId = faktura.TenantId, Tekst = $"Utkast for {AbonnementService.PeriodeTekst(faktura.PeriodeFra, faktura.PeriodeTil)} slettet", Ikon = "trash", Av = av });
        await db.SaveChangesAsync();
    }

    // Henter fakturanummer, forfall og betaling fra Tripletex for ordrene
    // som er sendt dit.
    public async Task OppdaterFraTripletexAsync()
    {
        var tx = await ItlockTripletexAsync();
        if (tx is null)
        {
            return;
        }

        var apne = await db.PlattformFakturaer
            .Where(f => (f.Status == PlattformFakturaStatus.SendtTilTripletex || f.Status == PlattformFakturaStatus.Fakturert) && f.TripletexOrdreNummer != null)
            .ToListAsync();
        if (apne.Count == 0)
        {
            return;
        }

        var kunder = await db.TenantAbonnementer.AsNoTracking()
            .Where(a => a.TripletexKundeId != null)
            .ToDictionaryAsync(a => a.TenantId, a => a.TripletexKundeId!.Value);

        foreach (var gruppe in apne.GroupBy(f => f.TenantId))
        {
            if (!kunder.TryGetValue(gruppe.Key, out var kundeId))
            {
                continue;
            }

            var fra = gruppe.Min(f => f.Opprettet).Date.AddDays(-7);
            var (fakturaer, feil) = await tx.HentFakturaerAsync(fra, DateTime.Today.AddDays(1), kundeId);
            if (feil is not null)
            {
                logger.LogWarning("Klarte ikke å hente fakturaer fra Tripletex: {Feil}", feil);
                continue;
            }

            foreach (var f in gruppe)
            {
                var treff = fakturaer.FirstOrDefault(t => !t.ErKreditnota && t.OrdreNummer == f.TripletexOrdreNummer);
                if (treff is null)
                {
                    continue;
                }

                f.TripletexFakturaNummer = treff.Nummer;
                f.Fakturadato = treff.Dato;
                f.Forfall = treff.Forfallsdato;
                f.Utestaende = treff.Utestaende;
                if (treff.Utestaende <= 0)
                {
                    if (f.Status != PlattformFakturaStatus.Betalt)
                    {
                        db.TenantHendelser.Add(new TenantHendelse { TenantId = f.TenantId, Tekst = $"Faktura {treff.Nummer} er betalt", Ikon = "check-circle" });
                    }

                    f.Status = PlattformFakturaStatus.Betalt;
                    f.BetaltDato ??= DateTime.Today;
                }
                else
                {
                    f.Status = PlattformFakturaStatus.Fakturert;
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static string Siffer(string? s) => new((s ?? "").Where(char.IsDigit).ToArray());
}

// Lager fakturautkast når en ny periode starter, og henter betalingsstatus
// fra Tripletex. Sending gjør du selv i adminportalen etter å ha sett over.
public class PlattformFakturaBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PlattformFakturaBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SjekkIntervall);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
                var abonnement = scope.ServiceProvider.GetRequiredService<AbonnementService>();

                // itlock fakturerer ikke seg selv, og bare aktive kunder faktureres.
                var kunder = await db.Tenants.AsNoTracking()
                    .Where(t => !t.ErStandard && t.Status == TenantStatus.Aktiv)
                    .ToListAsync(stoppingToken);
                foreach (var tenant in kunder)
                {
                    var abo = await abonnement.SikreAbonnementAsync(tenant);
                    var start = abo.ProveperiodeTil is { } p && p.Date >= abo.NesteFakturaFra.Date ? p.Date.AddDays(1) : abo.NesteFakturaFra.Date;
                    var harUtkast = await db.PlattformFakturaer.AnyAsync(f => f.TenantId == tenant.Id && f.Status == PlattformFakturaStatus.Utkast, stoppingToken);
                    if (start <= DateTime.Today && !harUtkast)
                    {
                        await abonnement.LagNesteFakturaAsync(tenant, "Automatisk");
                    }
                }

                await scope.ServiceProvider.GetRequiredService<PlattformFakturaService>().OppdaterFraTripletexAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under fakturering av organisasjonene.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
