using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Prismodell «Antall» for serviceavtaler (Prototype serviceavtale-ny-prising-tilbud.html):
/// kategorier fra Oppsett med antall per runde × pris, pluss tillegg (oppmøte,
/// kjøring, påslag …), ganger runder i året, minus rabatt. Avtalen starter som et
/// tilbud med fanene Prising, Tillegg og Tilbud, og blir en vanlig serviceavtale når
/// tilbudet godkjennes.
/// </summary>
public static class ServiceavtaleAntall
{
    public static readonly (string Navn, string Beskrivelse, decimal Pris, string Type)[] StandardKategorier =
    [
        ("Dørautomatikk", "Slagdørsautomatikk, åpner og lukker", 1250, TypeAutomatikk),
        ("Skyvedørsautomatikk", "Skyvedør med sensorer og nødåpning", 1650, TypeSkyvedor),
        ("Mekanisk", "Låskasse, sylinder, beslag og dørlukker", 350, TypeMekanisk),
        ("2-fløyet dør", "To dørblad, koordinator og stangbeslag", 600, TypeTofloyet),
        ("Adgangskontroll", "Kortleser, elektrisk lås og sentral", 900, TypeAdgang),
    ];

    // ---- Telling av dører når et prosjekt gjøres om til serviceavtale ----
    public const string TypeAutomatikk = "automatikk";
    public const string TypeSkyvedor = "skyvedor";
    public const string TypeTofloyet = "tofloyet";
    public const string TypeAdgang = "adgang";
    public const string TypeMekanisk = "mekanisk";

    public static readonly (string Kode, string Navn)[] KategoriTyper =
    [
        (TypeAutomatikk, "Dører med dørautomatikk"),
        (TypeSkyvedor, "Dører med skyvedørsautomatikk"),
        (TypeTofloyet, "Dører med panikkbeslag/koordinator"),
        (TypeAdgang, "Dører med kortleser"),
        (TypeMekanisk, "Mekaniske dører uten automatikk"),
    ];

    /// Typen kategorien teller: satt i Oppsett, ellers gjettet ut fra navnet.
    public static string? KategoriType(string? type, string navn)
    {
        if (type is not null)
        {
            return type.Length == 0 ? null : type; // "" = telles ikke
        }
        var n = navn.ToLowerInvariant();
        return n.Contains("skyvedør") ? TypeSkyvedor
            : n.Contains("automatikk") ? TypeAutomatikk
            : n.Contains("fløy") || n.Contains("panikk") ? TypeTofloyet
            : n.Contains("adgang") || n.Contains("kortleser") ? TypeAdgang
            : n.Contains("mekanisk") ? TypeMekanisk
            : null;
    }

    /// Forslag til sjekkpunkter for avtalen ut fra kategoriene som er i bruk på Prising.
    public static readonly Dictionary<string, string[]> StandardPunkter = new()
    {
        [TypeAutomatikk] = ["Sensorer og sikkerhetsfunksjoner på dørautomatikk", "Åpne-/lukkehastighet og kraft", "Nødåpning og batteri"],
        [TypeSkyvedor] = ["Sensorer og sikkerhetsfunksjoner på skyvedører", "Løpeskinner og gulvstyringer rengjort og smurt", "Nødåpning og batteri på skyvedører"],
        [TypeTofloyet] = ["Koordinator", "Panikkbeslag / stangbeslag", "Dørlukkere på begge fløyer"],
        [TypeAdgang] = ["Kortlesere", "El-sluttstykker og el-låser", "Dørkontakter og sentral"],
        [TypeMekanisk] = ["Låser og sylindre", "Dørlukkere justert", "Smøring av beslag og hengsler"],
    };

    private static readonly string[] MekaniskeTyper = ["Dørlukker", "Dørvrider", "Sylinder", "Mekanisk lås", "Elektrisk sluttstykke"];

    /// Kategoritypene én dør hører til, ut fra servicetypene på beslagene:
    /// skyvedørsautomatikk; dørautomatikk (slagdør); panikkbeslag/koordinator = 2-fløyet
    /// (og da ikke mekanisk); kortleser = adgangskontroll i tillegg; mekanisk = lukker,
    /// vrider, sylinder, lås eller el-sluttstykke uten automatikk.
    public static HashSet<string> DorTyper(IReadOnlySet<string> typer)
    {
        bool Har(string t) => typer.Contains(t);
        var res = new HashSet<string>();
        var skyvedor = Har("Skyvedørsautomatikk");
        var automatikk = Har("Dørautomatikk") && !skyvedor;
        var tofloyet = Har("Koordinator") || Har("Panikkbeslag");
        if (skyvedor) res.Add(TypeSkyvedor);
        if (automatikk) res.Add(TypeAutomatikk);
        if (tofloyet) res.Add(TypeTofloyet);
        if (Har("Kortleser")) res.Add(TypeAdgang);
        if (!skyvedor && !automatikk && !tofloyet && MekaniskeTyper.Any(Har)) res.Add(TypeMekanisk);
        return res;
    }

    /// Antall dører per kategoritype på et prosjekt.
    public static async Task<Dictionary<string, int>> TellDorer(ApplicationDbContext db, int prosjektId)
    {
        var typeKart = await ServiceRegelMotor.HentTypeKart(db);
        var dorer = await db.Dorer.AsNoTracking().Where(d => d.ProsjektId == prosjektId)
            .Include(d => d.Komponenter).ThenInclude(k => k.Component).ToListAsync();
        var svar = new Dictionary<string, int>();
        foreach (var d in dorer)
        {
            foreach (var t in DorTyper(ServiceRegelMotor.TyperPaDor(d, typeKart)))
            {
                svar[t] = svar.GetValueOrDefault(t) + 1;
            }
        }
        return svar;
    }

    public static readonly (string Navn, string Type, decimal? Pris)[] StandardTillegg =
    [
        ("Fast oppmøte", ServiceavtaleLinje.PerRunde, null),
        ("Kjøring", ServiceavtaleLinje.PerKm, null),
        ("Fast påslag", ServiceavtaleLinje.Prosent, null),
    ];

    public static readonly (string Kode, string Navn)[] Typer =
    [
        (ServiceavtaleLinje.PerRunde, "kr per runde"),
        (ServiceavtaleLinje.PerKm, "kr per km"),
        (ServiceavtaleLinje.PerAr, "kr per år"),
        (ServiceavtaleLinje.Prosent, "% påslag"),
    ];

    public static string TypeNavn(string? type) => Typer.FirstOrDefault(t => t.Kode == type).Navn ?? "kr per runde";

    // Valg for hva tilbuds-PDF-en viser.
    public const string VisLinjer = "linjer";
    public const string VisStk = "stk";
    public const string VisRunde = "runde";
    public const string VisMnd = "mnd";
    public const string VisSignatur = "signatur";
    public const string StandardValg = "linjer,runde,signatur";

    public static HashSet<string> Valg(Prosjekt p) =>
        (p.ServiceTilbudValg ?? StandardValg).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

    public sealed record Beregning(decimal PerRunde, decimal Kategorier, decimal Tillegg, decimal Brutto, decimal Rabatt, decimal Arspris, int Runder)
    {
        public decimal Mva => Math.Round(Arspris * 0.25m, 2);
        public decimal InklMva => Arspris + Mva;
        public decimal PerManed => Math.Round(Arspris / 12, 2);
    }

    /// Kategoriene per runde (antall × pris).
    public static decimal PerRunde(IEnumerable<ServiceavtaleLinje> linjer) =>
        linjer.Where(l => !l.ErTillegg).Sum(l => Math.Max(0, l.Antall) * Math.Max(0, l.Pris));

    /// Ett tillegg per år. Prosent regnes av kategoriene per år.
    public static decimal TilleggPerAr(ServiceavtaleLinje t, decimal kategorierPerAr, int runder) =>
        t.Pris <= 0 ? 0 : t.TilleggType switch
        {
            ServiceavtaleLinje.Prosent => Math.Round(kategorierPerAr * t.Pris / 100, 2),
            ServiceavtaleLinje.PerAr => t.Pris * Math.Max(0, t.Antall),
            _ => t.Pris * Math.Max(0, t.Antall) * runder
        };

    public static Beregning Beregn(Prosjekt p, IEnumerable<ServiceavtaleLinje> linjer)
    {
        var liste = linjer.ToList();
        var runder = ServiceavtalePris.RunderPerAr(p);
        var perRunde = PerRunde(liste);
        var kategorier = perRunde * runder;
        var tillegg = liste.Where(l => l.ErTillegg).Sum(t => TilleggPerAr(t, kategorier, runder));
        var brutto = kategorier + tillegg;
        var rabatt = p.ServiceRabattProsent is > 0 ? Math.Round(brutto * Math.Min(100, p.ServiceRabattProsent.Value) / 100, 2) : 0;
        return new Beregning(perRunde, kategorier, tillegg, brutto, rabatt, brutto - rabatt, runder);
    }

    /// Fyller inn standardkategorier og -tillegg i Oppsett første gang.
    public static async Task SikreStandard(ApplicationDbContext db)
    {
        if (!await db.ServicePrisKategorier.AnyAsync())
        {
            var i = 0;
            foreach (var (navn, besk, pris, type) in StandardKategorier)
            {
                db.ServicePrisKategorier.Add(new ServicePrisKategori { Navn = navn, Beskrivelse = besk, Pris = pris, Type = type, Rekkefolge = i++ });
            }
            var j = 0;
            if (!await db.ServiceTilleggMaler.AnyAsync())
            {
                foreach (var (navn, type, pris) in StandardTillegg)
                {
                    db.ServiceTilleggMaler.Add(new ServiceTilleggMal { Navn = navn, Type = type, Pris = pris, Rekkefolge = j++ });
                }
            }
            await db.SaveChangesAsync();
        }
    }

    /// Linjene en ny avtale får: kategoriene slik de står i Oppsett nå. Tillegg legges til
    /// fra + på Prising når de trengs.
    public static async Task<List<ServiceavtaleLinje>> LagLinjer(ApplicationDbContext db, int prosjektId, Dictionary<string, int>? antall = null)
    {
        await SikreStandard(db);
        var kat = await db.ServicePrisKategorier.AsNoTracking().OrderBy(k => k.Rekkefolge).ThenBy(k => k.Id).ToListAsync();
        var linjer = new List<ServiceavtaleLinje>();
        var r = 0;
        foreach (var k in kat)
        {
            var ant = antall is not null && KategoriType(k.Type, k.Navn) is { } kt ? antall.GetValueOrDefault(kt) : 0;
            linjer.Add(new ServiceavtaleLinje { ProsjektId = prosjektId, Navn = k.Navn, Beskrivelse = k.Beskrivelse, Pris = k.Pris, StandardPris = k.Pris, Antall = ant, Rekkefolge = r++ });
        }
        return linjer;
    }

    /// Prosjekt som er gjort om til serviceavtale (eller avtale uten noe fra før): blir et
    /// tilbud med prismodell «Antall», og Prising fylles inn med dørene telt per kategori.
    /// Avtaler som allerede har runder, pris eller prislinjer, røres ikke.
    public static async Task<bool> GjorOmTilTilbud(ApplicationDbContext db, Prosjekt p)
    {
        if (p.ServicePrismodell == ServiceavtalePris.Antall)
        {
            return false;
        }
        // Gjort om fra prosjekt nå (ServiceErTilbud satt da statusen ble endret): det nye
        // gjelder alltid, også om prosjektet har hatt runder før – de beholdes.
        var fraProsjekt = p.ServiceErTilbud;
        if (!fraProsjekt && (p.ServiceGodkjentArspris is not null
            || await db.Servicerunder.AnyAsync(r => r.ProsjektId == p.Id)
            || await db.ServiceKategoriPriser.AnyAsync(k => k.ProsjektId == p.Id)
            || await db.ServiceMinuttLinjer.AnyAsync(k => k.ProsjektId == p.Id)))
        {
            return false;
        }
        p.ServiceGodkjentArspris = null;
        p.ServiceGodkjentDato = null;
        p.ServicePrismodell = ServiceavtalePris.Antall;
        p.ServiceErTilbud = true;
        p.ServiceGangerPerAr ??= 1;
        if (!await db.ServiceavtaleLinjer.AnyAsync(l => l.ProsjektId == p.Id))
        {
            db.ServiceavtaleLinjer.AddRange(await LagLinjer(db, p.Id, await TellDorer(db, p.Id)));
        }
        await db.SaveChangesAsync();
        return true;
    }

    /// På en serviceavtale er alt allerede montert: alle dører står som ferdig montert
    /// og alle beslag vi monterer som montert.
    public static async Task MarkerMontert(ApplicationDbContext db, int prosjektId)
    {
        var na = DateTime.Now;
        await db.Dorer.Where(d => d.ProsjektId == prosjektId && d.Status != MontasjeStatus.FerdigMontert)
            .ExecuteUpdateAsync(x => x.SetProperty(d => d.Status, MontasjeStatus.FerdigMontert));
        await db.DorKomponenter.Where(k => k.Dor!.ProsjektId == prosjektId && !k.Montert)
            .ExecuteUpdateAsync(x => x
                .SetProperty(k => k.Montert, true)
                .SetProperty(k => k.MontertDato, k => k.MontertDato ?? na));
    }

    /// Bytter en eksisterende avtale (med runder eller pris fra før) til det nye oppsettet:
    /// prismodell «Antall», Prising fylt inn med dørene telt per kategori. Avtalen er
    /// allerede i drift, så den blir ikke et tilbud. Rundene og alt annet beholdes.
    public static async Task ByttTilNyttOppsett(ApplicationDbContext db, Prosjekt p)
    {
        if (p.ServicePrismodell == ServiceavtalePris.Antall)
        {
            return;
        }
        p.ServicePrismodell = ServiceavtalePris.Antall;
        p.ServiceErTilbud = false;
        p.ServiceGangerPerAr ??= 1;
        if (!await db.ServiceavtaleLinjer.AnyAsync(l => l.ProsjektId == p.Id))
        {
            db.ServiceavtaleLinjer.AddRange(await LagLinjer(db, p.Id, await TellDorer(db, p.Id)));
        }
        await db.SaveChangesAsync();
    }

    /// Fletter inn {kunde}, {bygg}, {adresse}, {runder} og {pris} i tilbudsteksten (HTML).
    public static string Flett(string html, Prosjekt p, Beregning b)
    {
        static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
        return html
            .Replace("{kunde}", E(p.Kunde?.Navn ?? "kunden"), StringComparison.OrdinalIgnoreCase)
            .Replace("{bygg}", E(p.Navn), StringComparison.OrdinalIgnoreCase)
            .Replace("{adresse}", E(p.Adresse), StringComparison.OrdinalIgnoreCase)
            .Replace("{runder}", b.Runder.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{pris}", Kr(b.Arspris), StringComparison.OrdinalIgnoreCase);
    }

    public static string Kr(decimal v) => Math.Round(v).ToString("#,##0", CultureInfo.GetCultureInfo("nb-NO")) + " kr";

    /// Standardteksten på tilbud for serviceavtale («Service- og vedlikeholdsavtale»).
    /// Firmanavnet settes inn der det står XXXX AS, og vakttelefonen fra firmaet hvis den finnes.
    /// Kunde, bygg, runder og datoer står fast på forsiden, og prisen i tabellen på side 2.
    public static string StandardTekst(string org, string? vakttelefon = null)
    {
        var f = System.Net.WebUtility.HtmlEncode(org);
        var tlf = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(vakttelefon) ? "XXX XX XXX" : vakttelefon);
        return
            "<h2>Service- og vedlikeholdsavtale</h2>" +
            "<p><em>(Gjelder dører og dørmiljø omfattet av avtalen)</em></p>" +
            "<h3>Omfang</h3>" +
            "<p>Denne service- og vedlikeholdsavtalen gjelder dører og dørmiljø som er mer merket med dørfunksjonskode «serviceavtale (SA), i Full Kontroll, og er ikke knyttet til prosjektleveranse, montasje, entrepriseforhold eller prosjektforutsetninger.</p>" +
            "<p>Formålet med avtalen er å sikre at dører, beslag og automatikker:</p>" +
            "<ul><li>fungerer korrekt og sikkert i daglig bruk</li>" +
            "<li>opprettholder krav til CE-samsvar, brann og rømning</li>" +
            "<li>vedlikeholdes i henhold til gjeldende regler, forskrifter og produsentenes anvisninger</li></ul>" +
            "<h3>Service og vedlikehold – omfatter</h3>" +
            $"<p>{f} forplikter seg til å utføre forebyggende vedlikehold og grunnservice i henhold til produsentens instruksjoner, gjeldende standarder og FDV.</p>" +
            "<p>Servicebesøk omfatter normalt følgende arbeider:</p>" +
            "<ul><li>Gjennomgang og mekanisk justering av skyvedørblader, slagdører og gulvstyringer</li>" +
            "<li>Justering av dørlukkere og dørautomatikker (åpnehastighet, lukkehastighet, kraft, tider m.m.)</li>" +
            "<li>Rengjøring og smøring av løpeskinner, drivverk, lås, beslag og bevegelige deler</li>" +
            "<li>Kontroll og justering av sensorer, sikkerhetsfunksjoner og styreenheter</li>" +
            "<li>Utskifting og reparasjon av defekte slitedeler innenfor avtalt omfang</li>" +
            "<li>Funksjonstest av dører, inkludert samspill mellom mekanikk, automatikk, lås og sikkerhetsfunksjoner</li></ul>" +
            $"<p>Periodisk vedlikehold utføres av {f} sine egne montører, eller av innleide montører direkte underlagt {f}, med nødvendig kompetanse og opplæring for å utføre arbeidet.</p>" +
            "<h3>CE-merking, brann og rømning</h3>" +
            "<p>Service og vedlikehold skal bidra til at:</p>" +
            "<ul><li>CE-merkede dører og automatikker opprettholder forutsetningene for sikker bruk</li>" +
            "<li>brann- og rømningsdører fungerer som prosjektert og ikke utgjør hinder for rømning</li>" +
            "<li>sikkerhetsfunksjoner ikke forringes over tid</li></ul>" +
            "<p>Avtalen omfatter kontroll og justering for å opprettholde funksjon og sikkerhet, men erstatter ikke krav til oppgraderinger eller endringer som følge av nye myndighetskrav.</p>" +
            "<h3>Vaktordning og responstid</h3>" +
            $"<p>Vakttelefon: {tlf}</p>" +
            $"<p>{f} skal være tilgjengelig for kontakt med teknisk personell på dagtid<br>(kl. 07:00/08:00 – 15:00/16:00).</p>" +
            $"<p>Ved akutt behov for å sikre personer eller bygget, skal {f} stille servicemontør på plass innen 3 timer, der dette er praktisk mulig.</p>" +
            "<p>Utenom ordinær arbeidstid (døgnservice) utføres kun nødreparasjoner for å sikre personer eller bygget.</p>" +
            "<p>Omfattende reparasjoner utføres ikke utenom ordinær arbeidstid med mindre dette er særskilt rekvirert.</p>" +
            "<h3>Feilmeldinger og historikk</h3>" +
            "<p>Alle feilmeldinger, servicebesøk og utskiftede komponenter loggføres fortløpende i Full Kontroll, slik at full historikk på alle dører i anlegget er tilgjengelig.</p>" +
            "<h3>Informasjon, avvik og større tiltak</h3>" +
            $"<p>{f} forplikter seg til å informere eier om:</p>" +
            "<ul><li>feil, slitasje eller forhold som faller utenfor avtalt serviceomfang</li>" +
            "<li>anbefalte reparasjoner eller forebyggende tiltak som bør utføres for å sikre tilfredsstillende drift og sikkerhet</li></ul>" +
            "<p>Større reparasjoner eller utskiftinger utføres kun etter avtale med eier.</p>" +
            $"<p>Dersom eier velger å ikke gjennomføre anbefalte tiltak som er nødvendige for sikker og forsvarlig drift, forbeholder {f} seg retten til å trekke aktuell dør eller anlegg ut av vedlikeholdsavtalen.</p>" +
            "<h3>Unntak og avgrensninger</h3>" +
            "<p>Avtalen omfatter ikke leveranser eller arbeider som følge av endrede forskrifter eller myndighetspålegg.</p>" +
            $"<ul><li>{f} er kun ansvarlig for utstyr og montasje utført av {f} eller våre samarbeidspartnere.</li>" +
            "<li>Skader som følge av ytre påvirkning, feil bruk, hærverk eller manglende tilgang omfattes ikke av avtalen.</li></ul>" +
            "<h3>Kontaktpersoner</h3>" +
            $"<p>{f} og kunde utveksler oversikt over kontaktpersoner med definerte ansvarsområder for drift, service og varsling.</p>" +
            "<h3>Rapportering i Full Kontroll</h3>" +
            "<p>Kunde får tilgang til Full Kontroll via driftslisens og har sanntidsoversikt over dører og servicepunkter.</p>" +
            "<p>I Full Kontroll vises blant annet:</p>" +
            "<ul><li>utførte serviceoppgaver i sanntid (24/7)</li>" +
            "<li>historikk per dør</li>" +
            "<li>anbefalte tiltak og deler som bør vurderes ved neste service</li></ul>";
    }
}
