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

        int tripletexKundeId;
        if (match is not null)
        {
            tripletexKundeId = match.Id;
        }
        else
        {
            // Kundenummeret stemmer ikke lenger med noen ekte kunde i
            // Tripletex (f.eks. slettet der, eller feltet ble aldri korrekt
            // satt) - oppretter kunden på nytt der i stedet for å feile, og
            // reparerer koblingen for neste gang.
            var (nyId, nyttKundenummer, opprettFeil) = await tripletex.OpprettKundeAsync(TilKundeOppdatering(kunde), ct);
            if (opprettFeil is not null || nyId is null)
            {
                ordre.TripletexOrdreFeil = $"Fant ikke kunden \"{kunde.Navn}\" igjen i Tripletex (kundenummer {kunde.TripletexKundenummer}), og fikk heller ikke opprettet den på nytt: {opprettFeil ?? sokFeil}";
                await db.SaveChangesAsync(ct);
                return (false, ordre.TripletexOrdreFeil);
            }

            kunde.TripletexKundenummer = nyttKundenummer ?? nyId.Value.ToString();
            await db.SaveChangesAsync(ct);
            tripletexKundeId = nyId.Value;
        }

        // Samme linjeberegning som portalens egne rapporter bruker (se
        // ArbeidsordreOkonomiBeregner) - ordren i Tripletex er kun "klar til
        // fakturering"-grunnlaget, ren fritekst pr. linje, uten kobling mot
        // Tripletex-produkter/-kontoer (det styres i portalen, se Inntektskonto).
        // Brukeren velger selv, i dialogen ved ferdigmelding, om alle linjene
        // skal med, om kun totalsummen skal overføres som én linje, eller om
        // dette er en akontofakturering med et manuelt beløp/kommentar.
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
            var sum = ArbeidsordreOkonomiBeregner.BeregnLinjer(ordre).Sum(l => l.Belop);
            var navn = ordre.Prosjekt?.Navn ?? ordre.Tittel;
            linjer = [new TripletexService.OrdreLinjeInput(navn, 1, sum)];
        }
        else
        {
            linjer = ArbeidsordreOkonomiBeregner.BeregnLinjer(ordre)
                .Select(l => new TripletexService.OrdreLinjeInput(
                    l.Navn, l.Antall, l.Antall == 0 ? 0 : l.Belop / l.Antall))
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
}
