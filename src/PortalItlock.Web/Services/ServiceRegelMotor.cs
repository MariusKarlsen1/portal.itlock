using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Setter servicekategori på dører ut fra prisreglene (ServicePrisRegel), og
/// regner ut prisen på en serviceavtale. Reglene ser på standard servicetyper
/// fra komponenttypene på døren (ikke produktnavn), og eventuelt på dørfunksjoner.
/// Den aktive regelen med lavest prioritet som treffer gjelder. Treffer flere
/// regler med samme laveste prioritet, er det en konflikt - døren telles likevel
/// bare én gang (den første regelen i listen brukes).
/// </summary>
public static class ServiceRegelMotor
{
    public static readonly string[] Servicetyper =
    [
        "Dørautomatikk", "Skyvedørsautomatikk", "Elektrisk sluttstykke", "Motorlås", "Magnetlås", "Kortleser", "Sentral",
        "Koordinator", "Panikkbeslag", "Nødutstyr", "Dørlukker", "Mekanisk lås", "Sylinder", "Holdemagnet", "Dørvrider", "Skilt"
    ];

    /// Servicetype utledet fra navnet på komponenttypen når den ikke er satt.
    /// "Sluttstykke 1" gir null fordi navnet ikke sier om det er elektrisk.
    public static string? StandardServicetype(string? komponenttype)
    {
        var n = (komponenttype ?? "").Trim().ToLowerInvariant();
        if (n.Length == 0) return null;
        if (n.Contains("skyvedør")) return "Skyvedørsautomatikk";
        if (n.Contains("koordinator")) return "Koordinator";
        if (n.Contains("sentral")) return "Sentral";
        if (n.Contains("dørautomatikk") || n.Contains("klemsikring") || n.Contains("kelmsikring")) return "Dørautomatikk";
        if (n.Contains("kortleser")) return "Kortleser";
        if (n.Contains("panikkbeslag")) return "Panikkbeslag";
        if (n.Contains("nødutstyr")) return "Nødutstyr";
        if (n.Contains("magnetlås")) return "Magnetlås";
        if (n.Contains("motorlås")) return "Motorlås";
        if (n.Contains("dørlukker")) return "Dørlukker";
        if (n.Contains("låskasse")) return "Mekanisk lås";
        if (n.Contains("sylinder") && !n.Contains("skrue")) return "Sylinder";
        if (n.Contains("holdemagnet")) return "Holdemagnet";
        if (n.Contains("vrider") || n.Contains("dørhåndtak")) return "Dørvrider";
        if (n.Contains("skilt")) return "Skilt";
        return null;
    }

    public static string? Servicetype(ComponentType? t) =>
        t is null ? null : string.IsNullOrWhiteSpace(t.Servicetype) ? StandardServicetype(t.Navn) : t.Servicetype.Trim();

    /// ComponentTypeId -> servicetype for hele organisasjonen.
    public static async Task<Dictionary<int, string?>> HentTypeKart(ApplicationDbContext db) =>
        (await db.ComponentTypes.AsNoTracking().ToListAsync()).ToDictionary(t => t.Id, Servicetype);

    public static HashSet<string> TyperPaDor(Dor d, IReadOnlyDictionary<int, string?> typeKart)
    {
        var typer = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in d.Komponenter)
        {
            var typeId = k.Component?.ComponentTypeId;
            if (typeId is { } id && typeKart.TryGetValue(id, out var st) && !string.IsNullOrWhiteSpace(st))
            {
                typer.Add(st);
            }
        }
        return typer;
    }

    public static List<string> FunksjonerPaDor(Dor d) => d.Funksjoner.Select(f => f.Navn.ToLowerInvariant()).ToList();

    public static bool Treffer(ServicePrisRegel r, ISet<string> typer, IReadOnlyList<string> funksjoner)
    {
        if (!r.Aktiv) return false;
        bool Oppfylt(ServicePrisRegelBetingelse b) => b.Felt == ServiceRegelFelt.Funksjon
            ? funksjoner.Any(f => f.Contains(b.Verdi.Trim().ToLowerInvariant()))
            : typer.Contains(b.Verdi.Trim());
        var har = r.Betingelser.Where(b => !b.ErIkke).ToList();
        var ikke = r.Betingelser.Where(b => b.ErIkke);
        if (r.BareDisse)
        {
            // Døren skal ha minst én av typene, og ingen typer utenfor listen.
            var tillatt = har.Where(b => b.Felt == ServiceRegelFelt.Servicetype)
                .Select(b => b.Verdi.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return tillatt.Count > 0 && typer.Count > 0 && typer.All(tillatt.Contains) && ikke.All(b => !Oppfylt(b));
        }
        // Krav-grupper: gruppe > 0 = «en av», alle gruppene må stemme («og»).
        var okHar = har.Count == 0 || (r.KreverAlle
            ? har.Where(b => b.Gruppe <= 0).All(Oppfylt) && har.Where(b => b.Gruppe > 0).GroupBy(b => b.Gruppe).All(g => g.Any(Oppfylt))
            : har.Any(Oppfylt));
        return okHar && ikke.All(b => !Oppfylt(b));
    }

    public sealed record Resultat(ServicePrisRegel? Regel, IReadOnlyList<ServicePrisRegel> Treff, IReadOnlyList<ServicePrisRegel> Konflikt, IReadOnlySet<string> Typer)
    {
        public string Kategori => Regel?.Navn ?? IkkeKategorisert;
        public bool HarKonflikt => Konflikt.Count > 1;
    }

    public const string IkkeKategorisert = "Ikke kategorisert";

    public static Resultat Klassifiser(Dor d, IReadOnlyList<ServicePrisRegel> regler, IReadOnlyDictionary<int, string?> typeKart)
    {
        var typer = TyperPaDor(d, typeKart);
        var funk = FunksjonerPaDor(d);
        var treff = regler.Where(r => Treffer(r, typer, funk)).OrderBy(r => r.Prioritet).ThenBy(r => r.Id).ToList();
        if (treff.Count == 0) return new Resultat(null, treff, [], typer);
        var forste = treff[0];
        var like = treff.Where(r => r.Prioritet == forste.Prioritet).ToList();
        return new Resultat(forste, treff, like.Count > 1 ? like : [], typer);
    }

    /// Lager standardreglene første gang. Navnene er de samme som de gamle faste
    /// kategoriene, så priser som allerede er satt på avtalene gjelder videre.
    public static async Task SikreStandardregler(ApplicationDbContext db)
    {
        if (await db.ServicePrisRegler.AnyAsync())
        {
            if (!await db.ServicePrisRegelVersjoner.AnyAsync())
            {
                await LagreVersjon(db, null);
            }
            return;
        }
        ServicePrisRegelBetingelse T(string v, int i) => new() { Felt = ServiceRegelFelt.Servicetype, Verdi = v, Rekkefolge = i };
        ServicePrisRegelBetingelse F(string v, int i) => new() { Felt = ServiceRegelFelt.Funksjon, Verdi = v, Rekkefolge = i };
        db.ServicePrisRegler.AddRange(
            new ServicePrisRegel { Navn = ServiceKategori.Automatikk, Prioritet = 1, KreverAlle = false, Minutter = 25, Undertekst = "inkl. resertifisering av CE", Betingelser = [T("Dørautomatikk", 0), F("automatikk", 1)] },
            new ServicePrisRegel { Navn = ServiceKategori.Romningsdorer, Prioritet = 2, KreverAlle = false, Minutter = 12, Betingelser = [T("Panikkbeslag", 0), T("Nødutstyr", 1), F("rømning", 2), F("panikk", 3)] },
            new ServicePrisRegel { Navn = ServiceKategori.ElektriskSluttstykke, Prioritet = 3, KreverAlle = false, Minutter = 12, Betingelser = [T("Elektrisk sluttstykke", 0), T("Kortleser", 1), T("Magnetlås", 2), T("Motorlås", 3), F("elektrisk sluttstykke", 4)] },
            new ServicePrisRegel { Navn = ServiceKategori.Mekanisk, Prioritet = 4, BareDisse = true, Minutter = 6, Betingelser = [T("Sylinder", 0), T("Dørvrider", 1), T("Skilt", 2), T("Mekanisk lås", 3)] });
        await db.SaveChangesAsync();
        await LagreVersjon(db, null);
    }

    // ---- Versjoner av prisreglene ----

    public sealed record RegelKopi(string Navn, int Prioritet, bool KreverAlle, decimal Pris, int Minutter, string? Undertekst, bool Aktiv, List<BetingelseKopi> Betingelser, bool BareDisse = false);
    public sealed record BetingelseKopi(string Felt, string Verdi, bool ErIkke, int Gruppe = 0);

    /// Lagrer en fast kopi av alle reglene som neste versjon og returnerer versjonsnummeret.
    public static async Task<int> LagreVersjon(ApplicationDbContext db, string? avNavn)
    {
        var regler = await HentRegler(db);
        var kopi = regler.Select(r => new RegelKopi(r.Navn, r.Prioritet, r.KreverAlle, r.Pris, r.Minutter, r.Undertekst, r.Aktiv,
            r.Betingelser.OrderBy(b => b.ErIkke).ThenBy(b => b.Rekkefolge).Select(b => new BetingelseKopi(b.Felt, b.Verdi, b.ErIkke, b.Gruppe)).ToList(), r.BareDisse)).ToList();
        var neste = (await db.ServicePrisRegelVersjoner.MaxAsync(v => (int?)v.Versjon) ?? 0) + 1;
        db.ServicePrisRegelVersjoner.Add(new ServicePrisRegelVersjon
        {
            Versjon = neste,
            AvNavn = avNavn,
            AntallRegler = kopi.Count,
            Innhold = JsonSerializer.Serialize(kopi)
        });
        await db.SaveChangesAsync();
        return neste;
    }

    public static async Task<int?> GjeldendeVersjon(ApplicationDbContext db) =>
        await db.ServicePrisRegelVersjoner.MaxAsync(v => (int?)v.Versjon);

    public static List<RegelKopi> LesVersjon(string innhold)
    {
        try
        {
            return JsonSerializer.Deserialize<List<RegelKopi>>(innhold) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static Task<List<ServicePrisRegel>> HentRegler(ApplicationDbContext db) =>
        db.ServicePrisRegler.Include(r => r.Betingelser).AsNoTracking().OrderBy(r => r.Prioritet).ThenBy(r => r.Id).ToListAsync();

    /// Dørene på avtalen med det regelmotoren trenger.
    public static Task<List<Dor>> HentDorer(ApplicationDbContext db, int prosjektId) =>
        db.Dorer.AsNoTracking().Where(d => d.ProsjektId == prosjektId)
            .Include(d => d.Komponenter).ThenInclude(k => k.Component)
            .Include(d => d.Funksjoner)
            .OrderBy(d => d.Etasje).ThenBy(d => d.Dornummer).ToListAsync();

    // ---- Pris ----

    public sealed record Beregning(
        decimal Grunnlag, decimal Tillegg, decimal PerRunde, int Runder, decimal EgneLinjer,
        decimal Arspris, int MinutterPerRunde, bool MinsteprisBrukt)
    {
        public decimal ArsprisInklMva => Arspris * 1.25m;
    }

    /// Pris per dør for en kategori: avtalens egen pris hvis satt, ellers regelens.
    public static decimal PrisForKategori(ServicePrisRegel r, IEnumerable<ServiceKategoriPris> avtalePriser) =>
        avtalePriser.FirstOrDefault(k => !k.ErEgendefinert && k.Kategori == r.Navn)?.Pris ?? r.Pris;

    public static Beregning Beregn(Prosjekt p, decimal grunnlagPerRunde, int minutterPerRunde, IEnumerable<ServiceKategoriPris> egneLinjer)
    {
        var tillegg = (p.ServiceOppmote ?? 0) + (p.ServiceReise ?? 0) + (p.ServiceFastTillegg ?? 0);
        var perRunde = grunnlagPerRunde + tillegg;
        var minste = p.ServiceMinstepris is > 0 && perRunde < p.ServiceMinstepris.Value;
        if (minste) perRunde = p.ServiceMinstepris!.Value;
        if (p.ServiceRabattProsent is > 0) perRunde *= 1 - Math.Min(100, p.ServiceRabattProsent.Value) / 100m;
        var runder = ServiceavtalePris.RunderPerAr(p);
        var egne = egneLinjer.Where(k => k.ErEgendefinert).Sum(k => (k.Pris ?? 0) * (k.ManueltAntall ?? 0) * (k.KravServicePrAar ?? 1));
        return new Beregning(grunnlagPerRunde, tillegg, Math.Round(perRunde, 2), runder, egne, Math.Round(perRunde * runder + egne, 2), minutterPerRunde, minste);
    }

    public sealed record KategoriAntall(string Navn, int Antall);

    public sealed record AvtaleStatus(
        IReadOnlyList<KategoriAntall> Kategorier, int IkkeKategorisert, int Konflikter,
        decimal Beregnet, decimal? Godkjent, DateTime? GodkjentDato, int Endringer, bool Regelmodell)
    {
        public bool PrisAvviker => Godkjent is { } g && Math.Abs(g - Beregnet) >= 0.5m;
    }

    /// Kategorier, beregnet og godkjent pris for en avtale - brukes på Oversikt.
    public static async Task<AvtaleStatus> HentAvtaleStatus(ApplicationDbContext db, Prosjekt p)
    {
        await SikreStandardregler(db);
        var regler = await HentRegler(db);
        var typeKart = await HentTypeKart(db);
        var dorer = await HentDorer(db, p.Id);
        var kategoriPriser = await db.ServiceKategoriPriser.AsNoTracking().Where(k => k.ProsjektId == p.Id).ToListAsync();
        var res = dorer.Select(d => (Dor: d, Res: Klassifiser(d, regler, typeKart))).ToList();
        var regelmodell = p.ServicePrismodell == ServiceavtalePris.Kategorisert;
        Beregning beregning;
        if (regelmodell)
        {
            var grunnlag = res.Where(x => x.Res.Regel is not null).Sum(x => PrisForKategori(x.Res.Regel!, kategoriPriser));
            beregning = Beregn(p, grunnlag, res.Where(x => x.Res.Regel is not null).Sum(x => x.Res.Regel!.Minutter), kategoriPriser);
        }
        else
        {
            var linjer = await db.ServiceMinuttLinjer.AsNoTracking().Where(l => l.ProsjektId == p.Id).ToListAsync();
            var min = ServiceavtalePris.MinutterPerRunde(linjer);
            beregning = Beregn(p, ServiceavtalePris.PrisPerRunde(min, p.ServiceTimepris), min, []);
        }
        var forrige = LesGrunnlag(p.ServiceGodkjentGrunnlag);
        var endringer = regelmodell && forrige.Count > 0
            ? res.Count(x => !forrige.TryGetValue(x.Dor.Id, out var k) || k != x.Res.Kategori) + forrige.Keys.Except(dorer.Select(d => d.Id)).Count()
            : 0;
        var kategorier = res.GroupBy(x => x.Res.Regel)
            .Where(g => g.Key is not null)
            .OrderBy(g => g.Key!.Prioritet).ThenBy(g => g.Key!.Id)
            .Select(g => new KategoriAntall(g.Key!.Navn, g.Count())).ToList();
        return new AvtaleStatus(kategorier, res.Count(x => x.Res.Regel is null), res.Count(x => x.Res.HarKonflikt),
            beregning.Arspris, p.ServiceGodkjentArspris, p.ServiceGodkjentDato, endringer, regelmodell);
    }

    /// Årlig avtaleverdi for mange avtaler på én gang (hovedoversikten): godkjent
    /// pris hvis den finnes, ellers beregnet med prismodellen på avtalen.
    public static async Task<Dictionary<int, (decimal Verdi, bool Godkjent)>> Arsverdier(ApplicationDbContext db, IReadOnlyCollection<Prosjekt> prosjekter)
    {
        var ider = prosjekter.Select(p => p.Id).ToList();
        var regler = await HentRegler(db);
        var typeKart = await HentTypeKart(db);
        var dorer = await db.Dorer.AsNoTracking().Where(d => ider.Contains(d.ProsjektId))
            .Include(d => d.Komponenter).ThenInclude(k => k.Component)
            .Include(d => d.Funksjoner).ToListAsync();
        var kategoriPriser = await db.ServiceKategoriPriser.AsNoTracking().Where(k => ider.Contains(k.ProsjektId)).ToListAsync();
        var linjer = await db.ServiceMinuttLinjer.AsNoTracking().Where(l => ider.Contains(l.ProsjektId)).ToListAsync();
        var svar = new Dictionary<int, (decimal, bool)>();
        foreach (var p in prosjekter)
        {
            if (p.ServiceGodkjentArspris is { } g)
            {
                svar[p.Id] = (g, true);
                continue;
            }
            var kp = kategoriPriser.Where(k => k.ProsjektId == p.Id).ToList();
            Beregning b;
            if (p.ServicePrismodell == ServiceavtalePris.Kategorisert)
            {
                var res = dorer.Where(d => d.ProsjektId == p.Id).Select(d => Klassifiser(d, regler, typeKart)).Where(r => r.Regel is not null).ToList();
                b = Beregn(p, res.Sum(r => PrisForKategori(r.Regel!, kp)), res.Sum(r => r.Regel!.Minutter), kp);
            }
            else
            {
                var min = ServiceavtalePris.MinutterPerRunde(linjer.Where(l => l.ProsjektId == p.Id));
                b = Beregn(p, ServiceavtalePris.PrisPerRunde(min, p.ServiceTimepris), min, []);
            }
            svar[p.Id] = (b.Arspris, false);
        }
        return svar;
    }

    public static string LagGrunnlag(IEnumerable<(int DorId, string Kategori)> dorer) =>
        JsonSerializer.Serialize(dorer.ToDictionary(d => d.DorId.ToString(CultureInfo.InvariantCulture), d => d.Kategori));

    public static Dictionary<int, string> LesGrunnlag(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return (JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [])
                .Where(kv => int.TryParse(kv.Key, out _)).ToDictionary(kv => int.Parse(kv.Key, CultureInfo.InvariantCulture), kv => kv.Value);
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
