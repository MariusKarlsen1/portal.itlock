using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Holder kunder, varer og fakturastatus like i portalen og Tripletex for alle
// organisasjoner som har Tripletex koblet. Tripletex ber om færrest mulig kall,
// så synken styres av hendelser i stedet for fast polling:
//
//  - Webhooks: Tripletex varsler /tripletex/webhook/{organisasjon} når kunder,
//    kontakter, produkter eller fakturaer endres. Organisasjonen synkes når det
//    har vært stille i 30 sekunder (TripletexWebhookKo).
//  - Endringer i portalen: hvert 10. minutt sjekkes databasen (uten API-kall)
//    for kunder og priskoder som venter på å bli sendt.
//  - Full synk som sikkerhetsnett: én gang i døgnet når webhooks virker, ellers
//    hver time. Den setter også opp webhooks på nytt.
//  - Produktlisten hentes med checksum (304 når den er uendret).
public class TripletexKundeSynkBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TripletexKundeSynkBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Tikk = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LokalSjekk = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan Ro = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FullMedWebhooks = TimeSpan.FromHours(24);
    private static readonly TimeSpan FullUtenWebhooks = TimeSpan.FromHours(1);

    private DateTime _sistLokalSjekk = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Tikk);
        while (!stoppingToken.IsCancellationRequested)
        {
            await KjorAsync(stoppingToken);
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

    private async Task KjorAsync(CancellationToken stoppingToken)
    {
        // Hvert minutt: bare organisasjoner med webhook-varsler. Hvert 10. minutt: alle.
        var alle = DateTime.UtcNow - _sistLokalSjekk >= LokalSjekk;
        var venter = TripletexWebhookKo.Venter();
        if (!alle && venter.Count == 0)
        {
            return;
        }
        if (alle)
        {
            _sistLokalSjekk = DateTime.UtcNow;
        }

        List<Tenant> leietakere;
        string portalAdresse;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            leietakere = await platformDb.Tenants.AsNoTracking()
                .Where(t => t.Status == TenantStatus.Aktiv).ToListAsync(stoppingToken);
            portalAdresse = (await scope.ServiceProvider.GetRequiredService<PlattformOppsettService>().HentAsync()).PortalAdresse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tripletex-synk: klarte ikke å hente organisasjonene.");
            return;
        }

        foreach (var t in leietakere.Where(t => alle || venter.Contains(t.Id)))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await SynkOrganisasjonAsync(t, alle, portalAdresse, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tripletex-synk feilet for {Organisasjon}.", t.Navn);
            }
        }
    }

    private async Task SynkOrganisasjonAsync(Tenant t, bool lokalSjekk, string portalAdresse, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var tripletex = scope.ServiceProvider.GetRequiredService<TripletexNokkelOppslag>().Lag(t);
        if (!tripletex.ErKonfigurert)
        {
            TripletexWebhookKo.Ta(t.Id, TimeSpan.Zero);
            return;
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(t.ConnectionString)
            .AddInterceptors(new SqliteUnicodeFunctionsInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var synk = new TripletexSyncService(db, tripletex);

        var tilstand = await db.TripletexSyncTilstand.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        var fullHver = tilstand?.WebhookAktiv == true ? FullMedWebhooks : FullUtenWebhooks;
        var full = lokalSjekk && (tilstand?.SistFullSynkUtc is not { } sist || DateTime.UtcNow - sist >= fullHver);

        var endring = TripletexWebhookKo.Ta(t.Id, Ro);
        if (full)
        {
            endring = TripletexEndring.Alt;
        }
        else if (lokalSjekk)
        {
            var (kunder, koder) = await synk.VenterLokaleEndringerAsync(ct);
            if (kunder) endring |= TripletexEndring.Kunder;
            if (koder) endring |= TripletexEndring.Varer;
        }
        if (endring == TripletexEndring.Ingen)
        {
            return;
        }

        if (endring.HasFlag(TripletexEndring.Kunder))
        {
            var r = await synk.SynkroniserKunderAsync(ct);
            if (r.PushetTilTripletex + r.HentetFraTripletex > 0 || r.Feil.Count > 0)
            {
                logger.LogInformation("Tripletex-kundesynk for {Organisasjon}: {Sendt} sendt, {Hentet} hentet, {Oppdatert} oppdatert, {Feil} feil.",
                    t.Navn, r.PushetTilTripletex, r.HentetFraTripletex, r.Oppdatert, r.Feil.Count);
            }
        }

        if (endring.HasFlag(TripletexEndring.Varer))
        {
            // Full synk uten checksum, så portalens varepriser også sendes.
            var varer = await synk.SynkroniserVarerOgKoderAsync(ct, brukChecksum: !full);
            if (varer.KoderSendt + varer.KoderHentet + varer.VarerHentet + varer.VarerOppdatert > 0 || varer.Feil.Count > 0)
            {
                logger.LogInformation("Tripletex-varesynk for {Organisasjon}: {KS} koder sendt, {KH} koder hentet, {VH} varer hentet, {VO} varer oppdatert, {Feil} feil.",
                    t.Navn, varer.KoderSendt, varer.KoderHentet, varer.VarerHentet, varer.VarerOppdatert, varer.Feil.Count);
            }
        }

        if (endring.HasFlag(TripletexEndring.Fakturaer))
        {
            var fakt = await synk.OppdaterFakturertStatusAsync(ct);
            if (fakt.ArbeidsordreFakturert + fakt.AkontoerBetalt > 0 || fakt.Feil is not null)
            {
                logger.LogInformation("Tripletex-fakturastatus for {Organisasjon}: {Ao} arbeidsordre fakturert, {Akonto} akontoer betalt{Feil}.",
                    t.Navn, fakt.ArbeidsordreFakturert, fakt.AkontoerBetalt, fakt.Feil is null ? "" : $", feil: {fakt.Feil}");
            }
        }

        if (full)
        {
            // Webhooks krever en offentlig https-adresse (ikke localhost).
            var adresse = $"{portalAdresse.TrimEnd('/')}/tripletex/webhook/{t.Id}";
            if (adresse.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !adresse.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                if (!await synk.SikreWebhooksAsync(adresse, ct))
                {
                    logger.LogWarning("Tripletex-webhooks kunne ikke settes opp for {Organisasjon}. Synker hver time i stedet.", t.Navn);
                }
            }

            var lagret = await db.TripletexSyncTilstand.FirstOrDefaultAsync(x => x.Id == 1, ct);
            if (lagret is null)
            {
                lagret = new TripletexSyncTilstand { Id = 1 };
                db.TripletexSyncTilstand.Add(lagret);
            }
            lagret.SistFullSynkUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
