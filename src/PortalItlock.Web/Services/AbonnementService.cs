using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Abonnementet til hver organisasjon: hva de har (lisenser, moduler,
// grunnpakke), hva det koster (prisliste eller avtalepris), og
// fakturagrunnlaget for neste periode. Brukes av adminportalen.
public class AbonnementService(PlatformDbContext db, PlattformOppsettService oppsett)
{
    private static readonly CultureInfo Nb = CultureInfo.GetCultureInfo("nb-NO");

    public sealed record Linje(
        string Nokkel, string Navn, string? Beskrivelse, PrisType Type,
        int Antall, decimal Listepris, decimal Pris, bool HarAvtalepris, int PakkeStorrelse = 1)
    {
        // Antall som faktureres: stykk, eller pakker (f.eks. 10 driftslisenser per pakke).
        public int Enheter => PakkeStorrelse > 1 ? (Antall + PakkeStorrelse - 1) / PakkeStorrelse : Antall;
        public decimal Sum => Enheter * Pris;
        public int? RabattProsent => Listepris > 0 && Pris < Listepris ? (int)Math.Round((1 - Pris / Listepris) * 100) : null;
    }

    // ---------- Prisliste ----------

    // Lager prislisten første gang, med lisensprisene som fantes fra før.
    public async Task SikrePrislisteAsync()
    {
        var finnes = await db.PrisProdukter.Select(p => p.Nokkel).ToListAsync();
        var gamle = await db.LisensPriser.ToListAsync();
        decimal Gammel(LisensType t) => gamle.FirstOrDefault(p => p.Type == t)?.KrPerManed ?? 0;

        var standard = new List<PrisProdukt>
        {
            new() { Nokkel = PrisProdukt.ForLisens(LisensType.KundeTilgang), Navn = "Kundetilganger", Beskrivelse = LisensType.KundeTilgang.Beskrivelse(), Type = PrisType.Lisens, Kr = Gammel(LisensType.KundeTilgang), Rekkefolge = 6 },
            new() { Nokkel = PrisProdukt.Etablering, Navn = "Etablering", Beskrivelse = "Oppsett, import og opplæring", Type = PrisType.Engang, Kr = 0, Rekkefolge = 20 },
        };
        // Én lisens per rolle. Prisen på den gamle felles brukerlisensen brukes
        // som startpris for admin, prosjektleder og montør.
        var brukerPris = await db.PrisProdukter.Where(p => p.Nokkel == PrisProdukt.ForLisens(LisensType.Brukere)).Select(p => (decimal?)p.Kr).FirstOrDefaultAsync() ?? Gammel(LisensType.Brukere);
        var rek = 2;
        foreach (var type in new[] { LisensType.Admin, LisensType.Prosjektleder, LisensType.Montor, LisensType.Driftslisens })
        {
            standard.Add(new PrisProdukt
            {
                Nokkel = PrisProdukt.ForLisens(type), Navn = type.Kortnavn(), Beskrivelse = type.Beskrivelse(), Type = PrisType.Lisens,
                Kr = type == LisensType.Driftslisens ? Gammel(LisensType.KundeTilgang) * 10 : brukerPris, Rekkefolge = rek++,
                PakkeStorrelse = type == LisensType.Driftslisens ? 10 : null
            });
        }

        var nr = 10;
        foreach (var modul in TilleggsModuler.Alle)
        {
            standard.Add(new PrisProdukt { Nokkel = PrisProdukt.ForModul(modul.Nokkel), Navn = modul.Navn, Beskrivelse = modul.Beskrivelse, Type = PrisType.Modul, Kr = 0, Rekkefolge = nr++ });
        }

        // Driftslisenser selges i pakker à 10. Første gang: prisen per stykk blir pris per pakke.
        var drift = await db.PrisProdukter.FirstOrDefaultAsync(p => p.Nokkel == PrisProdukt.ForLisens(LisensType.Driftslisens));
        if (drift is not null && drift.PakkeStorrelse is null)
        {
            drift.PakkeStorrelse = 10;
            drift.Kr *= 10;
            drift.Beskrivelse = "Vaktmestere og driftsansvarlige. Pakker à 10, én lisens kan følge flere bygg";
            foreach (var a in await db.TenantAvtalepriser.Where(a => a.Nokkel == drift.Nokkel).ToListAsync())
            {
                a.Kr *= 10;
            }

            await db.SaveChangesAsync();
        }

        if (!await db.PrisPakker.AnyAsync())
        {
            db.PrisPakker.AddRange(
                new PrisPakke { Nokkel = "start", Navn = "Start", Beskrivelse = "For de som vil komme i gang med prosjekter og tilbud", Innhold = "Prosjekter og tilbud\nKunderegister og kundekort\nBefaring og dørpakker", Rekkefolge = 1 },
                new PrisPakke { Nokkel = "pro", Navn = "Pro", Beskrivelse = "For firma med montører i felt", Innhold = "Alt i Start\nArbeidsordre og Min dag\nTimer, fravær og kalender", Rekkefolge = 2 },
                new PrisPakke { Nokkel = "komplett", Navn = "Komplett", Beskrivelse = "Hele livsløpet, fra tilbud til service", Innhold = "Alt i Pro\nService, serviceavtaler og FDV\nProsjektøkonomi og etterkalkyle\nKundeportal og driftsportal", Rekkefolge = 3 });
            await db.SaveChangesAsync();
        }

        var nye = standard.Where(p => !finnes.Contains(p.Nokkel)).ToList();
        if (nye.Count > 0)
        {
            db.PrisProdukter.AddRange(nye);
            await db.SaveChangesAsync();
        }

        // Avtalepriser på den gamle brukerlisensen flyttes til de nye, og den
        // gamle fjernes fra prislisten.
        var gammelNokkel = PrisProdukt.ForLisens(LisensType.Brukere);
        var gammelProdukt = await db.PrisProdukter.FirstOrDefaultAsync(p => p.Nokkel == gammelNokkel);
        if (gammelProdukt is not null)
        {
            var gamleAvtaler = await db.TenantAvtalepriser.Where(a => a.Nokkel == gammelNokkel).ToListAsync();
            foreach (var a in gamleAvtaler)
            {
                foreach (var type in LisensTypeExtensions.Ansatte)
                {
                    var nokkel = PrisProdukt.ForLisens(type);
                    if (!await db.TenantAvtalepriser.AnyAsync(x => x.TenantId == a.TenantId && x.Nokkel == nokkel))
                    {
                        db.TenantAvtalepriser.Add(new TenantAvtalepris { TenantId = a.TenantId, Nokkel = nokkel, Kr = a.Kr });
                    }
                }
            }

            db.TenantAvtalepriser.RemoveRange(gamleAvtaler);
            foreach (var e in await db.TenantAntallEndringer.Where(e => e.Nokkel == gammelNokkel && !e.Fakturert).ToListAsync())
            {
                e.Fakturert = true;
            }

            var kundeProdukt = await db.PrisProdukter.FirstOrDefaultAsync(p => p.Nokkel == PrisProdukt.ForLisens(LisensType.KundeTilgang));
            if (kundeProdukt is not null)
            {
                kundeProdukt.Beskrivelse = LisensType.KundeTilgang.Beskrivelse();
                kundeProdukt.Rekkefolge = 6;
            }

            db.PrisProdukter.Remove(gammelProdukt);
            await db.SaveChangesAsync();
        }

        // Grunnpakken er fjernet: kunden betaler etablering én gang og
        // deretter per lisens. Den fjernes fra prislisten, pakkene og avtalene.
        var grunnpakke = await db.PrisProdukter.FirstOrDefaultAsync(p => p.Nokkel == PrisProdukt.Grunnpakke);
        if (grunnpakke is not null)
        {
            db.PakkePriser.RemoveRange(await db.PakkePriser.Where(p => p.ProduktNokkel == PrisProdukt.Grunnpakke).ToListAsync());
            db.TenantAvtalepriser.RemoveRange(await db.TenantAvtalepriser.Where(a => a.Nokkel == PrisProdukt.Grunnpakke).ToListAsync());
            foreach (var e in await db.TenantAntallEndringer.Where(e => e.Nokkel == PrisProdukt.Grunnpakke && !e.Fakturert).ToListAsync())
            {
                e.Fakturert = true;
            }

            db.PrisProdukter.Remove(grunnpakke);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<PrisProdukt>> PrislisteAsync()
    {
        await SikrePrislisteAsync();
        return await db.PrisProdukter.OrderBy(p => p.Rekkefolge).ThenBy(p => p.Navn).ToListAsync();
    }

    // Holder den gamle LisensPris-tabellen i takt, siden den fortsatt leses
    // enkelte steder.
    public async Task LagrePrisAsync(PrisProdukt produkt)
    {
        foreach (var type in Enum.GetValues<LisensType>().Where(t => PrisProdukt.ForLisens(t) == produkt.Nokkel))
        {
            var gammel = await db.LisensPriser.FirstOrDefaultAsync(p => p.Type == type);
            if (gammel is not null)
            {
                gammel.KrPerManed = produkt.Kr;
            }
        }

        await db.SaveChangesAsync();
    }

    // ---------- Abonnement ----------

    public async Task<TenantAbonnement> SikreAbonnementAsync(Tenant tenant)
    {
        var abo = await db.TenantAbonnementer.FirstOrDefaultAsync(a => a.TenantId == tenant.Id);
        if (abo is null)
        {
            // Faktureres aldri bakover i tid: første periode er måneden etter
            // oppstart, eller neste måned for kunder som fantes fra før.
            var start = tenant.OpprettetDato.Date > DateTime.Today ? tenant.OpprettetDato.Date : DateTime.Today;
            abo = new TenantAbonnement
            {
                TenantId = tenant.Id,
                Startdato = tenant.OpprettetDato.Date,
                // Første faktura gjelder fra den 1. i måneden etter oppstart;
                // resten av oppstartsmåneden er gratis.
                NesteFakturaFra = new DateTime(start.Year, start.Month, 1).AddMonths(1),
                FakturaEpost = tenant.Epost
            };
            db.TenantAbonnementer.Add(abo);
            await db.SaveChangesAsync();
        }

        return abo;
    }

    public async Task<List<Linje>> LinjerAsync(int tenantId)
    {
        var produkter = await PrislisteAsync();
        var abo = await db.TenantAbonnementer.AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == tenantId);
        var lisenser = await db.TenantLisenser.AsNoTracking().Where(l => l.TenantId == tenantId).ToListAsync();
        var moduler = await db.TenantModuler.AsNoTracking().Where(m => m.TenantId == tenantId && m.Aktiv).Select(m => m.Modul).ToListAsync();
        var avtale = await db.TenantAvtalepriser.AsNoTracking().Where(a => a.TenantId == tenantId).ToDictionaryAsync(a => a.Nokkel, a => a.Kr);
        var pakkePriser = await db.PakkePriser.AsNoTracking().ToListAsync();
        return Bygg(produkter, abo, lisenser, moduler, avtale, pakkePriser);
    }

    private static List<Linje> Bygg(List<PrisProdukt> produkter, TenantAbonnement? abo, List<TenantLisens> lisenser, List<string> moduler, Dictionary<string, decimal> avtale, List<PakkePris> pakkePriser)
    {
        var linjer = new List<Linje>();
        foreach (var p in produkter.Where(p => p.Type != PrisType.Engang))
        {
            var antall = p.Type switch
            {
                PrisType.Fast => abo?.Grunnpakke == false ? 0 : 1,
                PrisType.Lisens => lisenser.FirstOrDefault(l => PrisProdukt.ForLisens(l.Type) == p.Nokkel)?.AntallTildelt ?? 0,
                PrisType.Modul => moduler.Any(m => PrisProdukt.ForModul(m) == p.Nokkel) ? 1 : 0,
                _ => 0
            };
            // Pris: avtalepris for kunden, ellers pakkens pris, ellers listeprisen.
            var liste = pakkePriser.FirstOrDefault(x => x.PakkeNokkel == abo?.Pakke && x.ProduktNokkel == p.Nokkel)?.Kr ?? p.Kr;
            var harAvtale = avtale.TryGetValue(p.Nokkel, out var avtalepris);
            linjer.Add(new Linje(p.Nokkel, p.Navn, p.Beskrivelse, p.Type, antall, liste, harAvtale ? avtalepris : liste, harAvtale, p.PakkeStorrelse ?? 1));
        }

        return linjer;
    }

    // Volumrabatt etter antall lisenser (stykk, ikke pakker), satt under Innstillinger.
    public static int RabattProsent(IEnumerable<Linje> linjer, PlattformInnstillinger inn)
    {
        if (!inn.VolumrabattAktiv)
        {
            return 0;
        }

        var antall = linjer.Where(l => l.Type == PrisType.Lisens && l.PakkeStorrelse <= 1).Sum(l => l.Antall);
        return antall >= inn.Rabatt2FraAntall && inn.Rabatt2FraAntall > 0 ? inn.Rabatt2Prosent
            : antall >= inn.Rabatt1FraAntall && inn.Rabatt1FraAntall > 0 ? inn.Rabatt1Prosent
            : 0;
    }

    public static decimal Rabattgrunnlag(IEnumerable<Linje> linjer) =>
        linjer.Where(l => l.Type == PrisType.Lisens && l.PakkeStorrelse <= 1 && !l.HarAvtalepris).Sum(l => l.Sum);

    public static decimal Maanedsbelop(IEnumerable<Linje> linjer, PlattformInnstillinger? inn = null)
    {
        var liste = linjer.ToList();
        var sum = liste.Sum(l => l.Sum);
        return inn is null ? sum : sum - Math.Round(Rabattgrunnlag(liste) * RabattProsent(liste, inn) / 100m, 2);
    }

    // Beløp per måned for alle organisasjoner (til oversiktene).
    public async Task<Dictionary<int, decimal>> MaanedsbelopForAlleAsync()
    {
        var produkter = await PrislisteAsync();
        var aboer = await db.TenantAbonnementer.AsNoTracking().ToListAsync();
        var lisenser = await db.TenantLisenser.AsNoTracking().ToListAsync();
        var moduler = await db.TenantModuler.AsNoTracking().Where(m => m.Aktiv).ToListAsync();
        var avtaler = await db.TenantAvtalepriser.AsNoTracking().ToListAsync();
        var ider = await db.Tenants.Select(t => t.Id).ToListAsync();

        var pakkePriser = await db.PakkePriser.AsNoTracking().ToListAsync();
        var inn = await oppsett.HentAsync();
        return ider.ToDictionary(id => id, id => Maanedsbelop(Bygg(produkter,
            aboer.FirstOrDefault(a => a.TenantId == id),
            lisenser.Where(l => l.TenantId == id).ToList(),
            moduler.Where(m => m.TenantId == id).Select(m => m.Modul).ToList(),
            avtaler.Where(a => a.TenantId == id).ToDictionary(a => a.Nokkel, a => a.Kr), pakkePriser), inn));
    }

    // Endrer antall (lisens), slår modul eller grunnpakke av/på. Økninger
    // logges, så de faktureres forholdsmessig på neste faktura.
    public async Task<string> SettAntallAsync(int tenantId, string nokkel, int nytt, string? av)
    {
        nytt = Math.Max(0, nytt);
        var produkt = await db.PrisProdukter.FirstAsync(p => p.Nokkel == nokkel);
        int gammelt;

        if (produkt.Type == PrisType.Lisens)
        {
            var type = Enum.GetValues<LisensType>().First(t => PrisProdukt.ForLisens(t) == nokkel);
            var lisens = await db.TenantLisenser.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Type == type);
            if (lisens is null)
            {
                lisens = new TenantLisens { TenantId = tenantId, Type = type };
                db.TenantLisenser.Add(lisens);
            }

            gammelt = lisens.AntallTildelt;
            lisens.AntallTildelt = nytt;
            lisens.SistEndret = DateTime.Now;
        }
        else if (produkt.Type == PrisType.Modul)
        {
            nytt = Math.Min(nytt, 1);
            var modulNokkel = nokkel["modul:".Length..];
            var modul = await db.TenantModuler.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Modul == modulNokkel);
            if (modul is null)
            {
                modul = new TenantModul { TenantId = tenantId, Modul = modulNokkel };
                db.TenantModuler.Add(modul);
            }

            gammelt = modul.Aktiv ? 1 : 0;
            modul.Aktiv = nytt == 1;
            modul.SistEndret = DateTime.Now;
            if (modul.Aktiv)
            {
                modul.AktivertDato ??= DateTime.Now;
            }
        }
        else
        {
            nytt = Math.Min(nytt, 1);
            var abo = await db.TenantAbonnementer.FirstAsync(a => a.TenantId == tenantId);
            gammelt = abo.Grunnpakke ? 1 : 0;
            abo.Grunnpakke = nytt == 1;
        }

        if (gammelt == nytt)
        {
            return "";
        }

        if (nytt > gammelt)
        {
            db.TenantAntallEndringer.Add(new TenantAntallEndring { TenantId = tenantId, Nokkel = nokkel, FraAntall = gammelt, TilAntall = nytt });
        }

        var tekst = produkt.Type == PrisType.Lisens
            ? $"{produkt.Navn}: {gammelt} → {nytt}"
            : $"{produkt.Navn} {(nytt == 1 ? "slått på" : "slått av")}";
        db.TenantHendelser.Add(new TenantHendelse { TenantId = tenantId, Tekst = tekst, Ikon = "credit-card", Av = av });
        await db.SaveChangesAsync();
        return tekst;
    }

    // null = bruk listeprisen.
    public async Task SettAvtaleprisAsync(int tenantId, string nokkel, decimal? kr, string? av)
    {
        var avtale = await db.TenantAvtalepriser.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Nokkel == nokkel);
        var produkt = await db.PrisProdukter.FirstAsync(p => p.Nokkel == nokkel);
        if (kr is null || kr == produkt.Kr)
        {
            if (avtale is not null)
            {
                db.TenantAvtalepriser.Remove(avtale);
                db.TenantHendelser.Add(new TenantHendelse { TenantId = tenantId, Tekst = $"{produkt.Navn}: tilbake til listepris {Kr(produkt.Kr)}", Ikon = "tag", Av = av });
            }
        }
        else
        {
            if (avtale is null)
            {
                avtale = new TenantAvtalepris { TenantId = tenantId, Nokkel = nokkel };
                db.TenantAvtalepriser.Add(avtale);
            }

            avtale.Kr = Math.Max(0, kr.Value);
            db.TenantHendelser.Add(new TenantHendelse { TenantId = tenantId, Tekst = $"{produkt.Navn}: avtalepris {Kr(avtale.Kr)}", Ikon = "tag", Av = av });
        }

        await db.SaveChangesAsync();
    }

    public async Task LoggAsync(int tenantId, string tekst, string ikon, string? av)
    {
        db.TenantHendelser.Add(new TenantHendelse { TenantId = tenantId, Tekst = tekst, Ikon = ikon, Av = av });
        await db.SaveChangesAsync();
    }

    // ---------- Fakturagrunnlag ----------

    public static string PeriodeTekst(DateTime fra, DateTime til) =>
        fra.Day == 1 && til == fra.AddMonths(1).AddDays(-1)
            ? fra.ToString("MMMM yyyy", Nb)
            : $"{fra:dd.MM.yyyy}–{til:dd.MM.yyyy}";

    private static string Kr(decimal v) => v.ToString("#,##0", Nb) + " kr";

    // Fakturagrunnlaget for neste periode, uten å lagre noe.
    public async Task<PlattformFaktura> ForhandsvisNesteAsync(Tenant tenant)
    {
        var abo = await SikreAbonnementAsync(tenant);
        var fra = abo.NesteFakturaFra.Date;
        if (abo.ProveperiodeTil is { } prove && prove.Date >= fra)
        {
            fra = prove.Date.AddDays(1);
        }

        var maneder = (int)abo.Intervall;
        var til = fra.AddMonths(maneder).AddDays(-1);
        var periode = PeriodeTekst(fra, til);
        var faktura = new PlattformFaktura { TenantId = tenant.Id, PeriodeFra = fra, PeriodeTil = til, Status = PlattformFakturaStatus.Utkast };
        var nr = 0;

        var inn = await oppsett.HentAsync();
        // Årlig forskudd: noen måneder gratis (Innstillinger).
        var betalteManeder = abo.Intervall == FakturaIntervall.Ar ? Math.Max(1, 12 - inn.GratisManeder) : maneder;
        var aarsTekst = abo.Intervall == FakturaIntervall.Ar && inn.GratisManeder > 0 ? $" (12 mnd, {inn.GratisManeder} gratis)" : "";
        var aktive = (await LinjerAsync(tenant.Id)).Where(l => l.Antall > 0 && l.Pris > 0).ToList();
        foreach (var l in aktive)
        {
            var hva = l.PakkeStorrelse > 1 ? $"{l.Navn}, {l.Enheter} {(l.Enheter == 1 ? "pakke" : "pakker")} à {l.PakkeStorrelse}" : l.Navn;
            faktura.Linjer.Add(new PlattformFakturaLinje { Tekst = $"{hva}, {periode}{aarsTekst}", Antall = l.Enheter, Pris = l.Pris * betalteManeder, Rekkefolge = nr++ });
        }

        var rabatt = RabattProsent(aktive, inn);
        var grunnlag = Rabattgrunnlag(aktive);
        if (rabatt > 0 && grunnlag > 0)
        {
            faktura.Linjer.Add(new PlattformFakturaLinje { Tekst = $"Volumrabatt {rabatt} % på lisenser", Antall = 1, Pris = -Math.Round(grunnlag * betalteManeder * rabatt / 100m, 2), Rekkefolge = nr++ });
        }

        if (!abo.EtableringFakturert)
        {
            var etablering = abo.Etableringsgebyr
                ?? (await db.PrisProdukter.AsNoTracking().FirstOrDefaultAsync(p => p.Nokkel == PrisProdukt.Etablering))?.Kr ?? 0;
            if (etablering > 0)
            {
                faktura.Linjer.Add(new PlattformFakturaLinje { Tekst = "Etablering: oppsett, import og opplæring", Antall = 1, Pris = etablering, Rekkefolge = nr++ });
            }
        }

        // Økninger etter forrige fakturerte periode startet, og før denne:
        // faktureres for dagene som gjenstod av forrige periode.
        // Bare dager som allerede er fakturert med det gamle antallet regnes med,
        // altså fra starten av forrige fakturerte periode.
        var forrigeFra = await db.PlattformFakturaer
            .Where(f => f.TenantId == tenant.Id && f.Status != PlattformFakturaStatus.Kreditert && f.PeriodeFra < fra)
            .Select(f => (DateTime?)f.PeriodeFra).MaxAsync();
        if (forrigeFra is { } forrigeStart)
        {
            var linjer = await LinjerAsync(tenant.Id);
            var endringer = await db.TenantAntallEndringer.AsNoTracking()
                .Where(e => e.TenantId == tenant.Id && !e.Fakturert)
                .ToListAsync();
            foreach (var e in endringer.Where(e => e.Dato.Date < fra).OrderBy(e => e.Dato))
            {
                var linje = linjer.FirstOrDefault(l => l.Nokkel == e.Nokkel);
                var fraDato = e.Dato.Date > forrigeStart ? e.Dato.Date : forrigeStart;
                var dager = (fra - fraDato).Days;
                if (linje is null || linje.Pris <= 0 || dager <= 0)
                {
                    continue;
                }

                var ekstra = linje.PakkeStorrelse > 1
                    ? (e.TilAntall + linje.PakkeStorrelse - 1) / linje.PakkeStorrelse - (e.FraAntall + linje.PakkeStorrelse - 1) / linje.PakkeStorrelse
                    : e.TilAntall - e.FraAntall;
                if (ekstra <= 0)
                {
                    continue;
                }

                var dagspris = Math.Round(linje.Pris * 12m / 365m * dager, 2);
                var hva = linje.Type == PrisType.Lisens ? $"{linje.Navn}, {ekstra} ekstra" : linje.Navn;
                faktura.Linjer.Add(new PlattformFakturaLinje
                {
                    Tekst = $"{hva} fra {fraDato:dd.MM} ({dager} dager)",
                    Antall = ekstra,
                    Pris = dagspris,
                    Rekkefolge = nr++
                });
            }
        }

        faktura.BelopEksMva = faktura.Linjer.Sum(l => l.Belop);
        return faktura;
    }

    // Lagrer neste faktura som utkast og flytter abonnementet til perioden etter.
    public async Task<PlattformFaktura?> LagNesteFakturaAsync(Tenant tenant, string? av)
    {
        var faktura = await ForhandsvisNesteAsync(tenant);
        if (faktura.Linjer.Count == 0)
        {
            return null;
        }

        var abo = await db.TenantAbonnementer.FirstAsync(a => a.TenantId == tenant.Id);
        db.PlattformFakturaer.Add(faktura);
        abo.NesteFakturaFra = faktura.PeriodeTil.AddDays(1);
        abo.EtableringFakturert = true;

        var endringer = await db.TenantAntallEndringer
            .Where(e => e.TenantId == tenant.Id && !e.Fakturert && e.Dato < faktura.PeriodeFra.AddDays(1))
            .ToListAsync();
        endringer.ForEach(e => e.Fakturert = true);

        db.TenantHendelser.Add(new TenantHendelse
        {
            TenantId = tenant.Id,
            Tekst = $"Faktura laget for {PeriodeTekst(faktura.PeriodeFra, faktura.PeriodeTil)} ({Kr(faktura.BelopEksMva)})",
            Ikon = "receipt",
            Av = av
        });
        await db.SaveChangesAsync();
        return faktura;
    }
}
