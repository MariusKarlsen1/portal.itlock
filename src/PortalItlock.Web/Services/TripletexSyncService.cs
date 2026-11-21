using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Koblingslogikken mellom portalens egne data (Kunder, Arbeidsordre) og
// Tripletex - selve HTTP-praten mot Tripletex ligger i TripletexService,
// denne klassen vet om databasen. Kunde.TripletexKundenummer er selve
// koblingsnøkkelen begge veier: en portal-kunde UTEN den er ikke sendt til
// Tripletex ennå, og en Tripletex-kunde uten en portal-kunde med matchende
// nummer er ikke hentet inn ennå - dette hindrer uendelige synk-løkker
// (A oppretter i B, B synker tilbake og oppretter en duplikat i A, osv.).
// Måten en arbeidsordre overføres til Tripletex på ved ferdigmelding - valgt
// av brukeren i en dialog som vises idet Ferdigmeld trykkes (se
// ArbeidsordreSkjema.razor).
public enum TripletexOverforingsType
{
    AlleLinjer,
    KunSum,
    Akonto
}

public sealed class TripletexSyncService(ApplicationDbContext db, TripletexService tripletex)
{
    public sealed record KundeSyncResultat(int PushetTilTripletex, int HentetFraTripletex, int Oppdatert, List<string> Feil);

    // Kjøres både fra bakgrunnsjobben (TripletexSyncBackgroundService) og kan
    // trigges manuelt fra /integrasjoner/tripletex.
    // Én kundesynk om gangen per organisasjon (database), så bakgrunnsjobben og
    // kundelisten ikke sender samme nye kunde til Tripletex to ganger.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> Laaser = new();

    private SemaphoreSlim Laas() => Laaser.GetOrAdd(db.Database.GetConnectionString() ?? "", _ => new SemaphoreSlim(1, 1));

    public async Task<KundeSyncResultat> SynkroniserKunderAsync(CancellationToken ct = default)
    {
        var laas = Laas();
        await laas.WaitAsync(ct);
        try
        {
            return await SynkroniserKunderInternAsync(ct);
        }
        finally
        {
            laas.Release();
        }
    }

    private async Task<KundeSyncResultat> SynkroniserKunderInternAsync(CancellationToken ct)
    {
        var feil = new List<string>();
        var pushet = 0;
        var hentet = 0;
        var oppdatert = 0;

        // Ny Tripletex-konto (ny testkonto, eller fra test til ekte Tripletex):
        // kundenumrene i portalen hører til den gamle kontoen og kan peke på helt
        // andre kunder i den nye. Koblingene nullstilles, og alle kundene sendes
        // til den nye kontoen (kobles til eksisterende med samme org.nr./navn).
        var konto = await tripletex.KontoAsync(ct);
        if (konto is null)
        {
            return new KundeSyncResultat(0, 0, 0, ["Fikk ikke kontakt med Tripletex. Sjekk nøklene under Oppsett → Tripletex."]);
        }
        var start = await db.TripletexSyncTilstand.FirstOrDefaultAsync(t => t.Id == 1, ct);
        if (start is null)
        {
            start = new TripletexSyncTilstand { Id = 1 };
            db.TripletexSyncTilstand.Add(start);
        }
        if (start.TripletexKonto is not null && start.TripletexKonto != konto)
        {
            await db.Kunder.ExecuteUpdateAsync(k => k.SetProperty(x => x.TripletexKundenummer, (string?)null).SetProperty(x => x.TripletexMaSendes, true), ct);
            start.SistSynkronisertKunderUtc = null;
        }
        start.TripletexKonto = konto;
        await db.SaveChangesAsync(ct);

        // 1) Portal -> Tripletex: nye kunder (uten kundenummer) og kunder som er
        //    endret i portalen siden sist (TripletexMaSendes). Kontaktpersonene
        //    sendes med. Kunder uten navn er ikke ferdig utfylt ennå.
        var aaSende = await db.Kunder
            .Include(k => k.Kontaktpersoner)
            .Where(k => k.Navn != "" && (k.TripletexKundenummer == null || k.TripletexMaSendes))
            .ToListAsync(ct);

        foreach (var k in aaSende)
        {
            var (ny, kundeFeil) = await SendKundeInternAsync(k, ct);
            if (kundeFeil is not null)
            {
                feil.Add(kundeFeil);
                continue;
            }
            if (ny)
            {
                pushet++;
            }
            else
            {
                oppdatert++;
            }
        }

        if (aaSende.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        // 2) Tripletex -> Portal: kunder opprettet/endret direkte i Tripletex
        //    sitt eget grensesnitt siden forrige synk.
        var tilstand = await db.TripletexSyncTilstand.FirstOrDefaultAsync(t => t.Id == 1, ct);
        var (tripletexKunder, hentFeil) = await tripletex.HentEndredeKunderAsync(tilstand?.SistSynkronisertKunderUtc, ct);
        if (hentFeil is not null)
        {
            feil.Add($"Kunne ikke hente kunder fra Tripletex: {hentFeil}");
        }
        else
        {
            // Kundenummeret er koblingsnøkkelen - slår opp portal-kunder som
            // allerede matcher NOEN av de returnerte Tripletex-kundene i ett
            // sett, i stedet for én spørring pr. kunde.
            var kundenumre = tripletexKunder
                .Select(tk => tk.CustomerNumber)
                .Where(n => n is not null)
                .Select(n => n!)
                .ToHashSet();

            var eksisterende = await db.Kunder
                .Include(k => k.Kontaktpersoner)
                .Where(k => k.TripletexKundenummer != null && kundenumre.Contains(k.TripletexKundenummer))
                .ToDictionaryAsync(k => k.TripletexKundenummer!, ct);

            foreach (var tk in tripletexKunder)
            {
                if (tk.CustomerNumber is null)
                {
                    continue;
                }

                Kunde kunde;
                if (eksisterende.TryGetValue(tk.CustomerNumber, out var portalKunde))
                {
                    // Endringer som ennå ikke er sendt fra portalen skal ikke overskrives.
                    if (portalKunde.TripletexMaSendes)
                    {
                        continue;
                    }
                    // Oppdater eksisterende. Tomme felt i Tripletex visker ikke ut
                    // det som står i portalen.
                    portalKunde.Navn = tk.Navn;
                    portalKunde.OrgNr = tk.OrganizationNumber ?? portalKunde.OrgNr;
                    portalKunde.Epost = tk.Email ?? portalKunde.Epost;
                    portalKunde.Telefon = tk.Telefon ?? portalKunde.Telefon;
                    portalKunde.Adresse = tk.Adresse ?? portalKunde.Adresse;
                    portalKunde.Postnr = tk.Postnr ?? portalKunde.Postnr;
                    portalKunde.Sted = tk.Sted ?? portalKunde.Sted;
                    kunde = portalKunde;
                    oppdatert++;
                }
                else
                {
                    kunde = new Kunde
                    {
                        Navn = tk.Navn,
                        OrgNr = tk.OrganizationNumber,
                        Epost = tk.Email,
                        Telefon = tk.Telefon,
                        Adresse = tk.Adresse,
                        Postnr = tk.Postnr,
                        Sted = tk.Sted,
                        Type = string.IsNullOrWhiteSpace(tk.OrganizationNumber) ? KundeType.Privatperson : KundeType.Bedriftskunde,
                        TripletexKundenummer = tk.CustomerNumber
                    };
                    db.Kunder.Add(kunde);
                    hentet++;
                }

                // Kontaktpersonene fra Tripletex som ikke finnes i portalen ennå.
                var (kontakter, _) = await tripletex.HentKontakterAsync(tk.Id, ct);
                foreach (var kt in kontakter)
                {
                    if (!kunde.Kontaktpersoner.Any(p => string.Equals(p.Navn.Trim(), kt.Navn, StringComparison.OrdinalIgnoreCase)))
                    {
                        kunde.Kontaktpersoner.Add(new KundeKontaktperson { Navn = kt.Navn, Epost = kt.Epost, Telefon = kt.Telefon });
                    }
                }
                if (string.IsNullOrWhiteSpace(kunde.Kontaktperson) && kontakter.Count > 0)
                {
                    kunde.Kontaktperson = kontakter[0].Navn;
                }
            }

            if (tilstand is null)
            {
                tilstand = new TripletexSyncTilstand { Id = 1 };
                db.TripletexSyncTilstand.Add(tilstand);
            }
            tilstand.SistSynkronisertKunderUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
        }

        return new KundeSyncResultat(pushet, hentet, oppdatert, feil);
    }

    // Sender én kunde til Tripletex med en gang (Lagre på kundekortet).
    public async Task<string?> SendKundeAsync(int kundeId, CancellationToken ct = default)
    {
        if (!tripletex.ErKonfigurert)
        {
            return null;
        }
        var laas = Laas();
        await laas.WaitAsync(ct);
        try
        {
            var kunde = await db.Kunder.Include(k => k.Kontaktpersoner).FirstOrDefaultAsync(k => k.Id == kundeId, ct);
            if (kunde is null || string.IsNullOrWhiteSpace(kunde.Navn))
            {
                return null;
            }
            // Kan ha blitt sendt av bakgrunnsjobben i mellomtiden.
            await db.Entry(kunde).ReloadAsync(ct);
            if (!kunde.TripletexMaSendes && !string.IsNullOrWhiteSpace(kunde.TripletexKundenummer))
            {
                return null;
            }
            var (_, kundeFeil) = await SendKundeInternAsync(kunde, ct);
            await db.SaveChangesAsync(ct);
            return kundeFeil;
        }
        finally
        {
            laas.Release();
        }
    }

    // Oppretter kunden i Tripletex (eller kobler den til en som finnes der fra
    // før med samme org.nr. eller navn), eller oppdaterer den, og sender
    // kontaktpersonene. Ny = true når kunden ble opprettet eller koblet nå.
    private async Task<(bool Ny, string? Feil)> SendKundeInternAsync(Kunde k, CancellationToken ct)
    {
        var ny = string.IsNullOrWhiteSpace(k.TripletexKundenummer);
        int? tripletexId = null;

        if (ny)
        {
            // Finnes firmaet allerede i Tripletex, kobles kunden til den i stedet
            // for å lage en kunde til.
            var funnet = !string.IsNullOrWhiteSpace(k.OrgNr) ? await tripletex.FinnKundeMedOrgNrAsync(k.OrgNr!, ct) : null;
            if (funnet is null)
            {
                var (treff, _) = await tripletex.SokKunderAsync(k.Navn, ct);
                funnet = treff.FirstOrDefault(t => string.Equals(t.Navn.Trim(), k.Navn.Trim(), StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(t.OrganizationNumber) || string.IsNullOrWhiteSpace(k.OrgNr)));
            }

            if (funnet is not null)
            {
                k.TripletexKundenummer = funnet.CustomerNumber ?? funnet.Id.ToString();
                tripletexId = funnet.Id;
                var oppdaterFeil = await tripletex.OppdaterKundeAsync(funnet.Id, TilKundeOppdatering(k), ct);
                if (oppdaterFeil is not null)
                {
                    return (true, $"Kunne ikke oppdatere \"{k.Navn}\" i Tripletex: {oppdaterFeil}");
                }
            }
            else
            {
                var (id, kundenummer, kundeFeil) = await tripletex.OpprettKundeAsync(TilKundeOppdatering(k), ct);
                if (kundeFeil is not null)
                {
                    return (true, $"Kunne ikke opprette \"{k.Navn}\" i Tripletex: {kundeFeil}");
                }
                k.TripletexKundenummer = kundenummer ?? id?.ToString();
                tripletexId = id;
            }
        }
        else
        {
            var (id, idFeil) = await TripletexKundeIdAsync(k, ct);
            if (id is null)
            {
                return (false, idFeil);
            }
            tripletexId = id;
            var oppdaterFeil = await tripletex.OppdaterKundeAsync(id.Value, TilKundeOppdatering(k), ct);
            if (oppdaterFeil is not null)
            {
                return (false, $"Kunne ikke oppdatere \"{k.Navn}\" i Tripletex: {oppdaterFeil}");
            }
        }

        if (tripletexId is { } tid)
        {
            var kontaktFeil = await SendKontakterAsync(k, tid, ct);
            if (kontaktFeil is not null)
            {
                return (ny, $"Kontaktpersonene til \"{k.Navn}\" ble ikke sendt til Tripletex: {kontaktFeil}");
            }
        }

        k.TripletexMaSendes = false;
        return (ny, null);
    }

    // Kontaktpersonene i portalen, og kundens kontaktperson, sendes til Tripletex.
    // De som finnes der fra før (samme navn) oppdateres. Ingen slettes i Tripletex.
    private async Task<string?> SendKontakterAsync(Kunde k, int tripletexKundeId, CancellationToken ct)
    {
        var personer = k.Kontaktpersoner
            .Where(p => !string.IsNullOrWhiteSpace(p.Navn))
            .Select(p => (Navn: p.Navn.Trim(), p.Epost, p.Telefon))
            .ToList();
        if (!string.IsNullOrWhiteSpace(k.Kontaktperson)
            && !personer.Any(p => string.Equals(p.Navn, k.Kontaktperson.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            personer.Insert(0, (k.Kontaktperson.Trim(), k.Epost, k.Telefon));
        }
        if (personer.Count == 0)
        {
            return null;
        }

        var (finnes, hentFeil) = await tripletex.HentKontakterAsync(tripletexKundeId, ct);
        if (hentFeil is not null)
        {
            return hentFeil;
        }
        foreach (var p in personer)
        {
            var match = finnes.FirstOrDefault(f => string.Equals(f.Navn, p.Navn, StringComparison.OrdinalIgnoreCase));
            if (match is not null && match.Epost == TripletexService.GyldigEpost(p.Epost) && match.Telefon == p.Telefon?.Trim())
            {
                continue;
            }
            var lagreFeil = await tripletex.LagreKontaktAsync(match?.Id, tripletexKundeId, p.Navn, p.Epost, p.Telefon, ct);
            if (lagreFeil is not null)
            {
                return lagreFeil;
            }
        }
        return null;
    }

    private static TripletexService.KundeOppdatering TilKundeOppdatering(Kunde k) => new(
        k.Navn, k.OrgNr, k.Epost, k.Telefon, k.Adresse, k.Postnr, k.Sted);

    // Kalles fra Ferdigmeld() på arbeidsordre-siden. Bygger ordrelinjer fra
    // BÅDE det opprinnelige tilbudet (hvis arbeidsordren stammer fra ett) OG
    // varer lagt til/byttet i felt (Arbeidsordre.Varer) - i motsetning til den
    // gamle CSV-eksporten (TripletexOrdreCsvService), som kun tok med
    // tilbudet og dermed ikke fanget opp endringer gjort etter at jobben var
    // i gang.
    public async Task<(bool Ok, string? Feilmelding)> PushArbeidsordreTilTripletexAsync(
        int arbeidsordreId,
        TripletexOverforingsType type = TripletexOverforingsType.AlleLinjer,
        decimal? akontoBelop = null,
        string? akontoKommentar = null,
        CancellationToken ct = default)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.Tilbud).ThenInclude(t => t!.Linjer).ThenInclude(l => l.Component)
            .Include(a => a.Varer).ThenInclude(v => v.Component)
            .Include(a => a.Tilbud!).ThenInclude(t => t.KodeLinjer).Include(a => a.KodeLinjer).Include(a => a.Timeregistreringer)
            .FirstOrDefaultAsync(a => a.Id == arbeidsordreId, ct);

        if (ordre is null)
        {
            return (false, "Fant ikke arbeidsordren.");
        }

        if (ordre.TripletexOrdreId is not null)
        {
            // Allerede sendt - ikke send på nytt (unngår duplikate ordre i
            // Tripletex hvis noen trykker Ferdigmeld flere ganger).
            return (true, null);
        }

        // Arbeidsordre.Kunde (satt fra tilbudet, eller valgt manuelt) er den
        // primære kilden - Prosjekt.Kunde er fallback for eldre arbeidsordre
        // fra før dette feltet fantes.
        var kunde = ordre.Kunde ?? ordre.Prosjekt?.Kunde;
        if (kunde is null)
        {
            ordre.TripletexOrdreFeil = "Arbeidsordren har ingen kunde satt (verken direkte eller via prosjekt).";
            await db.SaveChangesAsync(ct);
            return (false, ordre.TripletexOrdreFeil);
        }

        var (kundeId, kundeIdFeil) = await TripletexKundeIdAsync(kunde, ct);
        if (kundeId is not { } tripletexKundeId)
        {
            ordre.TripletexOrdreFeil = kundeIdFeil;
            await db.SaveChangesAsync(ct);
            return (false, ordre.TripletexOrdreFeil);
        }

        // Samme linjeberegning som portalens egne rapporter bruker (se
        // ArbeidsordreOkonomiBeregner) - ordren i Tripletex er kun "klar til
        // fakturering"-grunnlaget, ren fritekst pr. linje, uten kobling mot
        // Tripletex-produkter/-kontoer (det styres i portalen, se Inntektskonto).
        // Brukeren velger selv, i dialogen ved ferdigmelding, om alle linjene
        // skal med, om kun totalsummen skal overføres som én linje, eller om
        // dette er en akontofakturering med et manuelt beløp/kommentar.
        var koder = await db.PrisKoder.AsNoTracking().ToListAsync(ct);
        List<TripletexService.OrdreLinjeInput> linjer;
        if (type == TripletexOverforingsType.Akonto)
        {
            if (akontoBelop is null or <= 0)
            {
                ordre.TripletexOrdreFeil = "Akonto krever et beløp større enn 0.";
                await db.SaveChangesAsync(ct);
                return (false, ordre.TripletexOrdreFeil);
            }

            var navn = string.IsNullOrWhiteSpace(akontoKommentar) ? "Akonto" : $"Akonto - {akontoKommentar}";
            linjer = [new TripletexService.OrdreLinjeInput(navn, 1, akontoBelop.Value)];
        }
        else if (type == TripletexOverforingsType.KunSum)
        {
            var sum = ArbeidsordreOkonomiBeregner.BeregnLinjer(ordre, koder).Sum(l => l.Belop);
            var navn = ordre.Prosjekt?.Navn ?? ordre.Tittel;
            linjer = [new TripletexService.OrdreLinjeInput(navn, 1, sum)];
        }
        else
        {
            linjer = ArbeidsordreOkonomiBeregner.BeregnLinjer(ordre, koder)
                .Select(l =>
                {
                    if (l.Varenummer is not null)
                    {
                        return new TripletexService.OrdreLinjeInput(l.Navn, l.Antall, l.Antall == 0 ? 0 : l.Belop / l.Antall, l.Varenummer);
                    }
                    var t = ArbeidsordreOkonomiBeregner.TripletexLinje(l);
                    return new TripletexService.OrdreLinjeInput(t.Beskrivelse, l.Antall, t.Enhetspris, null, t.Innpris, t.RabattProsent);
                })
                .ToList();
        }

        var (ordreId, ordreNummer, ordreFeil) = await tripletex.OpprettOrdreAsync(
            tripletexKundeId, $"Arbeidsordre #{ordre.Id} - {ordre.Tittel}", linjer, ct);

        if (ordreFeil is not null)
        {
            ordre.TripletexOrdreFeil = ordreFeil;
            await db.SaveChangesAsync(ct);
            return (false, ordreFeil);
        }

        ordre.TripletexOrdreId = ordreId;
        ordre.TripletexOrdreNummer = ordreNummer;
        ordre.TripletexOrdreSendtDato = DateTime.Now;
        ordre.TripletexOrdreFeil = null;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Akonto fra faktureringsplanen under Prosjektøkonomi: én ordrelinje med
    // beløpet, på prosjektets kunde. Sendes bare én gang.
    public async Task<(bool Ok, string? Feilmelding)> PushAkontoTilTripletexAsync(int akontoId, decimal belop, decimal innestaaende = 0, CancellationToken ct = default)
    {
        var akonto = await db.ProsjektAkontoer.Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .FirstOrDefaultAsync(a => a.Id == akontoId, ct);
        if (akonto is null)
        {
            return (false, "Fant ikke akontoen.");
        }

        if (akonto.TripletexOrdreId is not null)
        {
            return (true, null);
        }

        if (belop <= 0)
        {
            return (false, "Akontoen har ikke noe beløp.");
        }

        if (akonto.Prosjekt?.Kunde is not { } kunde)
        {
            akonto.TripletexFeil = "Prosjektet har ingen kunde. Velg kunde på prosjektet først.";
            await db.SaveChangesAsync(ct);
            return (false, akonto.TripletexFeil);
        }

        var (kundeId, kundeFeil) = await TripletexKundeIdAsync(kunde, ct);
        if (kundeId is null)
        {
            akonto.TripletexFeil = kundeFeil;
            await db.SaveChangesAsync(ct);
            return (false, kundeFeil);
        }

        var tekst = $"Akonto {akonto.Nr} - {akonto.Milepael}";
        if (innestaaende > 0)
        {
            tekst += $" ({belop + innestaaende:N0} kr minus {innestaaende:N0} kr i innestående)";
        }

        var (ordreId, ordreNummer, ordreFeil) = await tripletex.OpprettOrdreAsync(
            kundeId.Value, $"{akonto.Prosjekt.Navn} - {tekst}", [new TripletexService.OrdreLinjeInput(tekst, 1, belop)], ct);
        if (ordreFeil is not null)
        {
            akonto.TripletexFeil = ordreFeil;
            await db.SaveChangesAsync(ct);
            return (false, ordreFeil);
        }

        akonto.TripletexOrdreId = ordreId;
        akonto.TripletexOrdreNummer = ordreNummer;
        akonto.TripletexFeil = null;
        akonto.Status = AkontoStatus.Sendt;
        akonto.SendtDato = DateTime.Now;
        akonto.SendtBelop = belop;
        akonto.SendtInnestaaende = innestaaende > 0 ? innestaaende : null;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Tripletex sin interne kunde-ID for kunden. Oppretter kunden i Tripletex
    // hvis den ikke finnes der ennå, og reparerer koblingen hvis den er brutt.
    private async Task<(int? KundeId, string? Feil)> TripletexKundeIdAsync(Kunde kunde, CancellationToken ct)
    {
        // Sørg for at kunden faktisk finnes i Tripletex først - oppretter den
        // der og gjenbruker koblingen (samme mekanisme som SynkroniserKunderAsync)
        // hvis den ikke allerede er synkronisert.
        if (string.IsNullOrWhiteSpace(kunde.TripletexKundenummer))
        {
            var (id, kundenummer, kundeFeil) = await tripletex.OpprettKundeAsync(TilKundeOppdatering(kunde), ct);
            if (kundeFeil is not null)
            {
                return (null, $"Fikk ikke opprettet kunden \"{kunde.Navn}\" i Tripletex: {kundeFeil}");
            }

            kunde.TripletexKundenummer = kundenummer ?? id?.ToString();
            await db.SaveChangesAsync(ct);
        }

        // TripletexKundenummer lagres som Tripletex sitt "customerNumber"
        // (visningsnummeret, f.eks. "10009") - IKKE den interne Tripletex-IDen
        // som kreves i order.customer.id. De to er helt forskjellige tall i
        // Tripletex sin datamodell (bekreftet i praksis: en tidligere versjon
        // her antok feilaktig at et tallformat TripletexKundenummer VAR IDen,
        // noe som sendte feil kunde-ID til Tripletex og ga 422 "Kunden finnes
        // ikke"). Slår derfor alltid opp den ekte IDen via søk, og matcher på
        // enten CustomerNumber (vanlig) eller Id (dekker det sjeldne
        // unntakstilfellet der OpprettKundeAsync måtte falle tilbake til å
        // lagre selve IDen fordi Tripletex ikke returnerte et kundenummer).
        var (treff, sokFeil) = await tripletex.SokKunderAsync(kunde.Navn, ct);
        var match = treff.FirstOrDefault(t =>
            t.CustomerNumber == kunde.TripletexKundenummer || t.Id.ToString() == kunde.TripletexKundenummer);

        if (match is not null)
        {
            return (match.Id, null);
        }

        // Kundenummeret stemmer ikke lenger med noen ekte kunde i
        // Tripletex (f.eks. slettet der, eller feltet ble aldri korrekt
        // satt) - oppretter kunden på nytt der i stedet for å feile, og
        // reparerer koblingen for neste gang.
        var (nyId, nyttKundenummer, opprettFeil) = await tripletex.OpprettKundeAsync(TilKundeOppdatering(kunde), ct);
        if (opprettFeil is not null || nyId is null)
        {
            return (null, $"Fant ikke kunden \"{kunde.Navn}\" igjen i Tripletex (kundenummer {kunde.TripletexKundenummer}), og fikk heller ikke opprettet den på nytt: {opprettFeil ?? sokFeil}");
        }

        kunde.TripletexKundenummer = nyttKundenummer ?? nyId.Value.ToString();
        await db.SaveChangesAsync(ct);
        return (nyId.Value, null);
    }
}
