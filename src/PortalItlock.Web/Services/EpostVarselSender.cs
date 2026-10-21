using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using static PortalItlock.Web.Services.EpostMal;

namespace PortalItlock.Web.Services;

// Sender e-postvarsler til brukerne: ny arbeidsordre tildelt (til montøren),
// ny forespørsel (til admin og prosjektledere), og påminnelse om å følge opp
// tilbud. Hvem som får hva, styres per rolle og per bruker i adminportalen
// (se EpostVarsler). E-posten kommer fra organisasjonens egen adresse og har
// felles utseende (EpostMal).
public class EpostVarselTjeneste(
    PlatformDbContext platformDb,
    ApplicationDbContext db,
    EmailService epost,
    PlattformOppsettService oppsett,
    ILogger<EpostVarselTjeneste> logger)
{
    private const string GrunnBruker = "Du får denne e-posten fordi du er bruker hos {0}. Administratoren kan endre hvilke e-postvarsler du får.";

    private async Task<(List<TenantVarselRolle> Roller, List<TenantBrukerVarsel> Brukere)> ValgAsync(int tenantId) =>
        (await platformDb.TenantVarselRoller.AsNoTracking().Where(v => v.TenantId == tenantId).ToListAsync(),
         await platformDb.TenantBrukerVarsler.AsNoTracking().Where(v => v.TenantId == tenantId).ToListAsync());

    private async Task<string> PortalAsync() => (await oppsett.HentAsync()).PortalAdresse.TrimEnd('/');

    private static string Grunn(Tenant org) => string.Format(GrunnBruker, org.PortalVisningsnavn ?? org.Navn);

    private static string Fornavn(string navn) => navn.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? navn;

    public async Task OrdreTildeltAsync(Tenant tenant, int ordreId, int brukerId)
    {
        var bruker = await db.Brukere.AsNoTracking().FirstOrDefaultAsync(b => b.Id == brukerId && b.Aktiv && b.SlettetDato == null);
        if (bruker is null || string.IsNullOrWhiteSpace(bruker.Epost))
        {
            return;
        }

        var (roller, brukere) = await ValgAsync(tenant.Id);
        if (!EpostVarsler.Gjelder(EpostVarsler.OrdreTildelt, bruker.Rolle, bruker.Id, roller, brukere))
        {
            return;
        }

        var ordre = await db.Arbeidsordre.AsNoTracking().Include(o => o.Kunde).FirstOrDefaultAsync(o => o.Id == ordreId);
        if (ordre is null || ordre.AnsvarligMontorId != brukerId)
        {
            return;
        }

        var fakta = new List<(string, string)>
        {
            ("Når", ordre.PlanlagtDato is { } d ? E(Dato(d)) : "Ikke planlagt ennå"),
        };
        if (!string.IsNullOrWhiteSpace(ordre.Adresse))
        {
            fakta.Add(("Hvor", $"<a href=\"{E(KartUrl(ordre.Adresse))}\" style=\"color:#1A1712;text-decoration:none\">{E(ordre.Adresse)}</a>"));
        }

        if (ordre.Kunde is { } k)
        {
            fakta.Add(("Kunde", E(k.Navn)));
        }

        if (ordre.EstimerteTimer is { } t && t > 0)
        {
            fakta.Add(("Estimert", $"{t.ToString("0.#", Nb)} timer"));
        }

        if (!string.IsNullOrWhiteSpace(ordre.Hentehylle))
        {
            fakta.Add(("Hentehylle", E(ordre.Hentehylle)));
        }

        var portal = await PortalAsync();
        var html = Lag(tenant, new Innhold
        {
            Farge = Gronn,
            Merke = "Ny arbeidsordre",
            Tittel = ordre.Tittel,
            IntroHtml = $"Hei {E(Fornavn(bruker.Navn))}, du har fått en ny jobb.",
            Fakta = fakta,
            Sitat = ordre.Beskrivelse,
            KnappTekst = "Åpne arbeidsordren",
            KnappUrl = $"{portal}/arbeidsordre/{ordre.Id}",
            Lenke2Tekst = string.IsNullOrWhiteSpace(ordre.Adresse) ? null : "Vis i kart",
            Lenke2Url = string.IsNullOrWhiteSpace(ordre.Adresse) ? null : KartUrl(ordre.Adresse),
            Tips = "Ordren ligger også i Min dag på mobilen, med kart, sjekkliste og timeføring.",
            Grunn = Grunn(tenant)
        });
        var emne = $"Ny arbeidsordre: {ordre.Tittel}{(ordre.PlanlagtDato is { } pd ? $", {pd.ToString("ddd d. MMM", Nb)}" : "")}";
        if (!await epost.SendAsync(bruker.Epost, emne, html, fraOrganisasjonId: tenant.Id))
        {
            logger.LogWarning("Kunne ikke sende varsel om arbeidsordre {OrdreId} til bruker {BrukerId}", ordreId, brukerId);
        }
    }

    public async Task NyForesporselAsync(Tenant tenant, int foresporselId)
    {
        var f = await db.Foresporsler.AsNoTracking().Include(x => x.Media).FirstOrDefaultAsync(x => x.Id == foresporselId);
        if (f is null)
        {
            return;
        }

        var (roller, brukere) = await ValgAsync(tenant.Id);
        var mottakere = (await db.Brukere.AsNoTracking()
                .Where(b => b.Aktiv && b.SlettetDato == null && b.Epost != "")
                .ToListAsync())
            .Where(b => EpostVarsler.Gjelder(EpostVarsler.ForesporselNy, b.Rolle, b.Id, roller, brukere))
            .Where(b => !string.Equals(b.Epost, f.FraEpost, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (mottakere.Count == 0)
        {
            return;
        }

        var avsender = string.IsNullOrWhiteSpace(f.FraNavn) ? f.FraEpost : f.FraNavn;
        var fraHtml = string.IsNullOrWhiteSpace(f.FraNavn) ? Epost(f.FraEpost) : $"{E(f.FraNavn)} · {Epost(f.FraEpost)}";
        var fakta = new List<(string, string)>
        {
            ("Fra", fraHtml),
            ("Kanal", f.Kanal switch
            {
                ForesporselKanal.Epost => "E-post",
                ForesporselKanal.Telefon => "Telefon",
                ForesporselKanal.Skjema => "Skjema på nettsiden",
                ForesporselKanal.Driftsportal => "Driftsportalen",
                _ => E(f.Kanal.ToString())
            }),
            ("Mottatt", E(Dato(f.MottattDato))),
        };
        if (f.Media.Count > 0)
        {
            fakta.Add(("Vedlegg", $"{f.Media.Count} vedlegg"));
        }

        var utdrag = f.Innhold.Length > 500 ? f.Innhold[..500].TrimEnd() + " …" : f.Innhold;
        var emne = string.IsNullOrWhiteSpace(f.Emne) ? "(uten emne)" : f.Emne;
        var html = Lag(tenant, new Innhold
        {
            Farge = Bla,
            Merke = "Ny forespørsel",
            Tittel = emne,
            IntroHtml = "Det har kommet en ny forespørsel som venter på svar.",
            Fakta = fakta,
            Sitat = string.IsNullOrWhiteSpace(utdrag) ? null : utdrag,
            KnappTekst = "Svar på forespørselen",
            KnappUrl = $"{await PortalAsync()}/foresporsler",
            Tips = "Svarer du fra portalen, kommer svaret fra organisasjonens adresse, og hele samtalen lagres på forespørselen.",
            Grunn = Grunn(tenant)
        });
        foreach (var b in mottakere)
        {
            await epost.SendAsync(b.Epost, $"Ny forespørsel fra {avsender}: {emne}", html, fraOrganisasjonId: tenant.Id);
        }
    }

    // Kjøres jevnlig: tilbud som står som sendt, der oppfølgingsdagen er nådd.
    public async Task TilbudOppfolgingAsync(Tenant tenant)
    {
        if (DateTime.Now.Hour < 8)
        {
            return;
        }

        var idag = DateTime.Today;
        var kandidater = await db.Tilbud
            .Where(t => t.Status == EndringsmeldingStatus.SendtTilKunde && t.OppfolgingDager != null && !t.OppfolgingSendt
                && t.SendtDato != null && t.OppfolgingBrukerId != null)
            .Include(t => t.Prosjekt)
            .ToListAsync();
        var forfalt = kandidater.Where(t => t.SendtDato!.Value.Date.AddDays(t.OppfolgingDager!.Value) <= idag).ToList();
        if (forfalt.Count == 0)
        {
            return;
        }

        var (roller, brukere) = await ValgAsync(tenant.Id);
        var portal = await PortalAsync();
        foreach (var t in forfalt)
        {
            var bruker = await db.Brukere.AsNoTracking().FirstOrDefaultAsync(b => b.Id == t.OppfolgingBrukerId && b.Aktiv && b.SlettetDato == null);
            if (bruker is not null && !string.IsNullOrWhiteSpace(bruker.Epost)
                && EpostVarsler.Gjelder(EpostVarsler.TilbudOppfolging, bruker.Rolle, bruker.Id, roller, brukere))
            {
                var dager = (idag - t.SendtDato!.Value.Date).Days;
                var fakta = new List<(string, string)> { ("Tilbud", E(t.Tittel)) };
                if (t.Prosjekt is { } p)
                {
                    fakta.Add(("Prosjekt", E(p.Navn)));
                }

                if (t.SendtSum is { } sum)
                {
                    fakta.Add(("Sum", $"{sum.ToString("#,##0", Nb)} kr eks. mva"));
                }

                fakta.Add(("Sendt", E(Dato(t.SendtDato.Value.Date))));
                if (t.GyldigTil is { } gyldig)
                {
                    var igjen = (gyldig.Date - idag).Days;
                    fakta.Add(("Gyldig til", $"{E(gyldig.ToString("d. MMMM", Nb))}{(igjen >= 0 ? $" ({igjen} {(igjen == 1 ? "dag" : "dager")} igjen)" : " (utløpt)")}"));
                }

                if (!string.IsNullOrWhiteSpace(t.KontaktpersonNavn))
                {
                    var kontakt = E(t.KontaktpersonNavn) + (string.IsNullOrWhiteSpace(t.KontaktpersonTelefon) ? "" : " · " + Telefon(t.KontaktpersonTelefon));
                    fakta.Add(("Kontakt", kontakt));
                }

                var harTlf = !string.IsNullOrWhiteSpace(t.KontaktpersonTelefon);
                var html = Lag(tenant, new Innhold
                {
                    Farge = Brun,
                    Merke = "Påminnelse",
                    Tittel = "Tid for å følge opp tilbudet",
                    IntroHtml = $"Hei {E(Fornavn(bruker.Navn))}, det er {dager} {(dager == 1 ? "dag" : "dager")} siden tilbudet ble sendt, og kunden har ikke svart ennå.",
                    Fakta = fakta,
                    KnappTekst = "Åpne tilbudet",
                    KnappUrl = $"{portal}/tilbud/{t.Id}",
                    Lenke2Tekst = harTlf ? "Ring kunden" : null,
                    Lenke2Url = harTlf ? $"tel:{t.KontaktpersonTelefon!.Replace(" ", "")}" : null,
                    Tips = "Når kunden svarer, sett status til Godkjent eller Avslått på tilbudet.",
                    Grunn = Grunn(tenant)
                });
                await epost.SendAsync(bruker.Epost, $"Følg opp: {t.Tittel} ({dager} {(dager == 1 ? "dag" : "dager")} siden)", html, fraOrganisasjonId: tenant.Id);
            }

            t.OppfolgingSendt = true;
        }

        await db.SaveChangesAsync();
    }
}

// Leser hendelsene fra EpostVarselKo og sender varslene, hver i et scope satt
// til riktig organisasjon. Sjekker også oppfølging av tilbud hver halvtime.
public class EpostVarselSender(IServiceScopeFactory scopeFactory, OrganisasjonKjorer kjorer, ILogger<EpostVarselSender> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(LesHendelserAsync(stoppingToken), TilbudOppfolgingAsync(stoppingToken));

    private async Task LesHendelserAsync(CancellationToken ct)
    {
        await foreach (var h in EpostVarselKo.Leser.ReadAllAsync(ct))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
                var tenant = await platformDb.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.ConnectionString == h.ConnectionString && t.Status == TenantStatus.Aktiv, ct);
                if (tenant is null)
                {
                    continue;
                }

                scope.ServiceProvider.GetRequiredService<ITenantContext>().Bruk(tenant);
                var tjeneste = scope.ServiceProvider.GetRequiredService<EpostVarselTjeneste>();
                if (h.Nokkel == EpostVarsler.OrdreTildelt && h.BrukerId is { } brukerId)
                {
                    await tjeneste.OrdreTildeltAsync(tenant, h.Id, brukerId);
                }
                else if (h.Nokkel == EpostVarsler.ForesporselNy)
                {
                    await tjeneste.NyForesporselAsync(tenant, h.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "E-postvarsel {Nokkel} for {Id} feilet.", h.Nokkel, h.Id);
            }
        }
    }

    private async Task TilbudOppfolgingAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        do
        {
            try
            {
                await kjorer.ForHverAsync("Oppfølging av tilbud", (sp, tenant) =>
                    sp.GetRequiredService<EpostVarselTjeneste>().TilbudOppfolgingAsync(tenant), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feil under oppfølging av tilbud.");
            }
        }
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false) && !ct.IsCancellationRequested);
    }
}
