using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Abonnementet til hver organisasjon: hva de har (lisenser, moduler,
// grunnpakke), hva det koster (prisliste eller avtalepris), og
// fakturagrunnlaget for neste periode. Brukes av adminportalen.
public class AbonnementService(PlatformDbContext db)
{
    private static readonly CultureInfo Nb = CultureInfo.GetCultureInfo("nb-NO");

    public sealed record Linje(
        string Nokkel, string Navn, string? Beskrivelse, PrisType Type,
        int Antall, decimal Listepris, decimal Pris, bool HarAvtalepris)
    {
        public decimal Sum => Antall * Pris;
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
            new() { Nokkel = PrisProdukt.Grunnpakke, Navn = "Grunnpakke", Beskrivelse = "Prosjekter, tilbud, service, kundeportal og Prosjektøkonomi", Type = PrisType.Fast, Kr = 0, Rekkefolge = 1 },
            new() { Nokkel = PrisProdukt.ForLisens(LisensType.Brukere), Navn = "Brukere", Beskrivelse = "Ansatte med innlogging", Type = PrisType.Lisens, Kr = Gammel(LisensType.Brukere), Rekkefolge = 2 },
            new() { Nokkel = PrisProdukt.ForLisens(LisensType.KundeTilgang), Navn = "Kundetilganger", Beskrivelse = "Kunder og vaktmestere i kundeportalen", Type = PrisType.Lisens, Kr = Gammel(LisensType.KundeTilgang), Rekkefolge = 3 },
            new() { Nokkel = PrisProdukt.Etablering, Navn = "Etablering", Beskrivelse = "Oppsett, import og opplæring", Type = PrisType.Engang, Kr = 0, Rekkefolge = 20 },
        };
        var nr = 10;
        foreach (var modul in TilleggsModuler.Alle)
        {
            standard.Add(new PrisProdukt { Nokkel = PrisProdukt.ForModul(modul.Nokkel), Navn = modul.Navn, Beskrivelse = modul.Beskrivelse, Type = PrisType.Modul, Kr = 0, Rekkefolge = nr++ });
        }

        var nye = standard.Where(p => !finnes.Contains(p.Nokkel)).ToList();
        if (nye.Count > 0)
        {
            db.PrisProdukter.AddRange(nye);
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
        return Bygg(produkter, abo, lisenser, moduler, avtale);
    }

    private static List<Linje> Bygg(List<PrisProdukt> produkter, TenantAbonnement? abo, List<TenantLisens> lisenser, List<string> moduler, Dictionary<string, decimal> avtale)
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
            var harAvtale = avtale.TryGetValue(p.Nokkel, out var avtalepris);
            linjer.Add(new Linje(p.Nokkel, p.Navn, p.Beskrivelse, p.Type, antall, p.Kr, harAvtale ? avtalepris : p.Kr, harAvtale));
        }

        return linjer;
    }

    public static decimal Maanedsbelop(IEnumerable<Linje> linjer) => linjer.Sum(l => l.Sum);

    // Beløp per måned for alle organisasjoner (til oversiktene).
    public async Task<Dictionary<int, decimal>> MaanedsbelopForAlleAsync()
    {
        var produkter = await PrislisteAsync();
        var aboer = await db.TenantAbonnementer.AsNoTracking().ToListAsync();
        var lisenser = await db.TenantLisenser.AsNoTracking().ToListAsync();
        var moduler = await db.TenantModuler.AsNoTracking().Where(m => m.Aktiv).ToListAsync();
        var avtaler = await db.TenantAvtalepriser.AsNoTracking().ToListAsync();
        var ider = await db.Tenants.Select(t => t.Id).ToListAsync();

        return ider.ToDictionary(id => id, id => Maanedsbelop(Bygg(produkter,
            aboer.FirstOrDefault(a => a.TenantId == id),
            lisenser.Where(l => l.TenantId == id).ToList(),
            moduler.Where(m => m.TenantId == id).Select(m => m.Modul).ToList(),
            avtaler.Where(a => a.TenantId == id).ToDictionary(a => a.Nokkel, a => a.Kr))));
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

        foreach (var l in (await LinjerAsync(tenant.Id)).Where(l => l.Antall > 0 && l.Pris > 0))
        {
            faktura.Linjer.Add(new PlattformFakturaLinje { Tekst = $"{l.Navn}, {periode}", Antall = l.Antall, Pris = l.Pris * maneder, Rekkefolge = nr++ });
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

                var ekstra = e.TilAntall - e.FraAntall;
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
