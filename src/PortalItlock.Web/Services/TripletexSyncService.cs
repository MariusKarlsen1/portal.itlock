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
public sealed class TripletexSyncService(ApplicationDbContext db, TripletexService tripletex)
{
    public sealed record KundeSyncResultat(int PushetTilTripletex, int HentetFraTripletex, int Oppdatert, List<string> Feil);

    // Kjøres både fra bakgrunnsjobben (TripletexSyncBackgroundService) og kan
    // trigges manuelt fra /integrasjoner/tripletex.
    public async Task<KundeSyncResultat> SynkroniserKunderAsync(CancellationToken ct = default)
    {
        var feil = new List<string>();
        var pushet = 0;
        var hentet = 0;
        var oppdatert = 0;

        // 1) Portal -> Tripletex: kunder opprettet i portalen som ikke er
        //    sendt til Tripletex ennå (mangler TripletexKundenummer).
        var nyePortalKunder = await db.Kunder
            .Where(k => k.TripletexKundenummer == null)
            .ToListAsync(ct);

        foreach (var k in nyePortalKunder)
        {
            var (id, kundenummer, kundeFeil) = await tripletex.OpprettKundeAsync(TilKundeOppdatering(k), ct);
            if (kundeFeil is not null)
            {
                feil.Add($"Kunne ikke opprette \"{k.Navn}\" i Tripletex: {kundeFeil}");
                continue;
            }

            k.TripletexKundenummer = kundenummer ?? id?.ToString();
            pushet++;
        }

        if (pushet > 0)
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
                .Where(k => k.TripletexKundenummer != null && kundenumre.Contains(k.TripletexKundenummer))
                .ToDictionaryAsync(k => k.TripletexKundenummer!, ct);

            foreach (var tk in tripletexKunder)
            {
                if (tk.CustomerNumber is null)
                {
                    continue;
                }

                if (eksisterende.TryGetValue(tk.CustomerNumber, out var portalKunde))
                {
                    // Oppdater eksisterende - "helt likt" betyr feltene skal
                    // holdes i synk, ikke bare opprettes én gang.
                    portalKunde.Navn = tk.Navn;
                    portalKunde.OrgNr = tk.OrganizationNumber;
                    portalKunde.Epost = tk.Email;
                    portalKunde.Telefon = tk.Telefon;
                    portalKunde.Adresse = tk.Adresse;
                    portalKunde.Postnr = tk.Postnr;
                    portalKunde.Sted = tk.Sted;
                    oppdatert++;
                }
                else
                {
                    db.Kunder.Add(new Kunde
                    {
                        Navn = tk.Navn,
                        OrgNr = tk.OrganizationNumber,
                        Epost = tk.Email,
                        Telefon = tk.Telefon,
                        Adresse = tk.Adresse,
                        Postnr = tk.Postnr,
                        Sted = tk.Sted,
                        TripletexKundenummer = tk.CustomerNumber
                    });
                    hentet++;
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

    private static TripletexService.KundeOppdatering TilKundeOppdatering(Kunde k) => new(
        k.Navn, k.OrgNr, k.Epost, k.Telefon, k.Adresse, k.Postnr, k.Sted);

    // Oppretter/oppdaterer Tripletex-produktet for en vare (Component) som har
    // fått satt en inntektskonto (Component.TripletexKontoId) - selve
    // mekanismen som gjør at salg av varen faktisk bokføres på riktig konto
    // når ordren senere faktureres i Tripletex. Kalles lat, kun når varen
    // faktisk selges (fra PushArbeidsordreTilTripletexAsync), ikke proaktivt
    // for alle varer - de fleste varer får aldri satt en inntektskonto.
    public async Task<string?> SynkroniserProduktAsync(Component c, CancellationToken ct = default)
    {
        if (c.TripletexKontoId is null)
        {
            return null;
        }

        var oppdatering = new TripletexService.ProduktOppdatering(
            c.Navn, c.Produktkode, c.PrisVeiledende ?? c.PrisNetto, c.TripletexKontoId.Value);

        var (id, feil) = await tripletex.OpprettEllerOppdaterProduktAsync(c.TripletexProduktId, oppdatering, ct);
        if (feil is not null)
        {
            return feil;
        }

        c.TripletexProduktId = id;
        await db.SaveChangesAsync(ct);
        return null;
    }

    // Kalles fra Ferdigmeld() på arbeidsordre-siden. Bygger ordrelinjer fra
    // BÅDE det opprinnelige tilbudet (hvis arbeidsordren stammer fra ett) OG
    // varer lagt til/byttet i felt (Arbeidsordre.Varer) - i motsetning til den
    // gamle CSV-eksporten (TripletexOrdreCsvService), som kun tok med
    // tilbudet og dermed ikke fanget opp endringer gjort etter at jobben var
    // i gang.
    public async Task<(bool Ok, string? Feilmelding)> PushArbeidsordreTilTripletexAsync(int arbeidsordreId, CancellationToken ct = default)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.Tilbud).ThenInclude(t => t!.Linjer).ThenInclude(l => l.Component)
            .Include(a => a.Varer).ThenInclude(v => v.Component)
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

        var kunde = ordre.Prosjekt?.Kunde;
        if (kunde is null)
        {
            ordre.TripletexOrdreFeil = "Arbeidsordren er ikke koblet til et prosjekt med kunde.";
            await db.SaveChangesAsync(ct);
            return (false, ordre.TripletexOrdreFeil);
        }

        // Sørg for at kunden faktisk finnes i Tripletex først - oppretter den
        // der og gjenbruker koblingen (samme mekanisme som SynkroniserKunderAsync)
        // hvis den ikke allerede er synkronisert.
        if (string.IsNullOrWhiteSpace(kunde.TripletexKundenummer))
        {
            var (id, kundenummer, kundeFeil) = await tripletex.OpprettKundeAsync(TilKundeOppdatering(kunde), ct);
            if (kundeFeil is not null)
            {
                ordre.TripletexOrdreFeil = $"Fikk ikke opprettet kunden \"{kunde.Navn}\" i Tripletex: {kundeFeil}";
                await db.SaveChangesAsync(ct);
                return (false, ordre.TripletexOrdreFeil);
            }

            kunde.TripletexKundenummer = kundenummer ?? id?.ToString();
            await db.SaveChangesAsync(ct);
        }

        if (!int.TryParse(kunde.TripletexKundenummer, out var tripletexKundeId))
        {
            // TripletexKundenummer kan i praksis være selve Tripletex-IDen
            // (satt av OpprettKundeAsync over hvis Tripletex ikke returnerte et
            // eget kundenummer) eller et rent visningsnummer - slår opp den
            // ekte IDen via søk hvis feltet ikke er tallformat.
            var (treff, sokFeil) = await tripletex.SokKunderAsync(kunde.Navn, ct);
            var match = treff.FirstOrDefault(t => t.CustomerNumber == kunde.TripletexKundenummer);
            if (match is null)
            {
                ordre.TripletexOrdreFeil = sokFeil ?? $"Fant ikke kunden \"{kunde.Navn}\" igjen i Tripletex (kundenummer {kunde.TripletexKundenummer}).";
                await db.SaveChangesAsync(ct);
                return (false, ordre.TripletexOrdreFeil);
            }
            tripletexKundeId = match.Id;
        }

        // Cacher produkt-synk pr. Component-ID innenfor dette pushet, slik at
        // samme vare brukt på flere linjer bare synkroniseres én gang.
        var produktCache = new Dictionary<int, int?>();
        async Task<int?> ProduktIdForAsync(Component? c)
        {
            if (c is null || c.TripletexKontoId is null)
            {
                return null;
            }
            if (produktCache.TryGetValue(c.Id, out var cachet))
            {
                return cachet;
            }
            await SynkroniserProduktAsync(c, ct);
            produktCache[c.Id] = c.TripletexProduktId;
            return c.TripletexProduktId;
        }

        var linjer = new List<TripletexService.OrdreLinjeInput>();

        if (ordre.Tilbud is not null)
        {
            var tilbudLinjer = ordre.Tilbud.Linjer
                .Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering)
                .OrderBy(l => l.Rekkefolge);
            foreach (var l in tilbudLinjer)
            {
                var produktId = await ProduktIdForAsync(l.Component);
                linjer.Add(new TripletexService.OrdreLinjeInput(l.Navn, l.Antall, l.Utpris, produktId));
            }

            var minutter = ordre.Tilbud.Linjer.Where(l => l.LevertAv == LevertAv.F).Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
            var arbeidstidTimer = ordre.Tilbud.EstimertTimerOverride ?? (minutter / 60m);
            var kalkulertMontasjekost = Math.Round(ordre.Tilbud.Timepris * arbeidstidTimer, 2);
            var montasjekost = ordre.Tilbud.Montasjekost ?? kalkulertMontasjekost;
            if (montasjekost > 0)
            {
                linjer.Add(new TripletexService.OrdreLinjeInput("Montasje", 1, montasjekost));
            }
        }
        else if (ordre.Timepris is not null && ordre.EstimerteTimer is not null)
        {
            linjer.Add(new TripletexService.OrdreLinjeInput("Montasje/arbeid", 1, ordre.Timepris.Value * ordre.EstimerteTimer.Value));
        }

        foreach (var v in ordre.Varer)
        {
            var produktId = await ProduktIdForAsync(v.Component);
            linjer.Add(new TripletexService.OrdreLinjeInput(v.Navn, v.Antall, v.Utpris, produktId));
        }

        var (ordreId, ordreNummer, ordreFeil) = await tripletex.OpprettOrdreAsync(
            tripletexKundeId, ordre.Tittel, $"Arbeidsordre #{ordre.Id}", linjer, ct);

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
}
