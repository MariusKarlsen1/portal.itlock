using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Fase 4 serviceavtaler: servicerunder som føres dør for dør. Ny runde tar et
/// fast øyeblikksbilde av dørene (rom, plassering på tegningen, kategori og
/// komponenter) og lager kontrollpunkter ut fra servicetypene på hver dør.
/// Det finnes aldri mer enn én pågående runde per avtale.
/// </summary>
public static class ServicerundeMotor
{
    public const string Generelt = "Generelt";

    private static readonly (string Type, string[] Punkter)[] Standard =
    [
        ("Dørautomatikk", ["Funksjonstest åpne og lukke", "Kontroll av sikkerhetssensorer", "Kontroll av aktivering", "Kontroll av lukkehastighet", "Kontroll av innfesting", "Kontroll av mekanisk funksjon"]),
        ("Elektrisk sluttstykke", ["Funksjonstest låsing og opplåsing", "Kontroll av sluttstykkets funksjon", "Kontroll av mekanisk inngrep", "Kontroll av tilkoblinger"]),
        ("Motorlås", ["Funksjonstest motorlås", "Kontroll av låsefunksjon", "Kontroll av tilbakemeldinger", "Kontroll av mekanisk funksjon"]),
        ("Magnetlås", ["Funksjonstest magnetlås", "Kontroll av holdekraft"]),
        ("Kortleser", ["Funksjonstest kortleser"]),
        ("Panikkbeslag", ["Åpner uten nøkkel (NS-EN 1125 / 179)", "Skilting og fri rømningsvei"]),
        ("Nødutstyr", ["Funksjonstest nødåpning"]),
        ("Dørlukker", ["Kontroll av lukkehastighet", "Døren lukker helt og går i lås"]),
        ("Mekanisk lås", ["Kontroll av mekanisk funksjon", "Smøring av låskasse og sluttstykke"]),
        ("Sylinder", ["Sylinderen går lett"]),
        ("Holdemagnet", ["Holdemagneten slipper ved brannalarm"]),
        (Generelt, ["Døren lukker og låser", "Generell kontroll og justering"]),
    ];

    public static async Task SikreKontrollpunkter(ApplicationDbContext db)
    {
        if (await db.ServiceKontrollpunkter.AnyAsync()) return;
        foreach (var (type, punkter) in Standard)
        {
            for (var i = 0; i < punkter.Length; i++)
            {
                db.ServiceKontrollpunkter.Add(new ServiceKontrollpunkt { Servicetype = type, Tekst = punkter[i], Rekkefolge = i });
            }
        }
        await db.SaveChangesAsync();
    }

    /// Kontrollpunktene for en dør: punktene for hver servicetype på døren, i
    /// fast rekkefølge, der like punkter slås sammen (kilden viser alle typene).
    /// Dører uten servicetyper får de generelle punktene.
    public static List<(string Tekst, string Kilde)> PunkterFor(IEnumerable<string> typer, IReadOnlyList<ServiceKontrollpunkt> mal)
    {
        var ordnet = ServiceRegelMotor.Servicetyper.Where(t => typer.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
        if (ordnet.Count == 0) ordnet.Add(Generelt);
        var liste = new List<(string Tekst, List<string> Kilder)>();
        foreach (var t in ordnet)
        {
            foreach (var p in mal.Where(m => m.DorId == null && m.Servicetype.Equals(t, StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Rekkefolge))
            {
                var finnes = liste.FirstOrDefault(x => x.Tekst.Equals(p.Tekst, StringComparison.OrdinalIgnoreCase));
                if (finnes.Tekst is null) liste.Add((p.Tekst, [t]));
                else if (!finnes.Kilder.Contains(t)) finnes.Kilder.Add(t);
            }
        }
        if (liste.Count == 0 && ordnet[0] != Generelt)
        {
            return PunkterFor([], mal);
        }
        return liste.Select(x => (x.Tekst, string.Join(" + ", x.Kilder))).ToList();
    }

    public sealed record Punkt(string Tekst, string Kilde, string? Standard, bool KreverFoto);

    /// Sjekklisten for én dør: felles punkter for servicetypene på døren (med krav
    /// og fotokrav), pluss faste tillegg som er lagt på akkurat denne døren.
    public const string AlleDorer = "Alle dører";

    /// Felles punkter for en avtale: standardpunktene (uten de som er skrudd av
    /// på avtalen) og avtalens egne punkter per servicetype.
    public static List<ServiceKontrollpunkt> FellesFor(IReadOnlyList<ServiceKontrollpunkt> mal, int? prosjektId)
    {
        var standard = mal.Where(m => m.DorId == null && m.ProsjektId == null).ToList();
        if (prosjektId is null) return standard;
        var av = mal.Where(m => m.ProsjektId == prosjektId && m.SkruddAv).ToList();
        return standard
            .Where(s => !av.Any(a => a.Servicetype.Equals(s.Servicetype, StringComparison.OrdinalIgnoreCase) && a.Tekst.Equals(s.Tekst, StringComparison.OrdinalIgnoreCase)))
            .Concat(mal.Where(m => m.ProsjektId == prosjektId && !m.SkruddAv && m.DorId == null && m.Servicetype != AlleDorer).OrderBy(m => m.Rekkefolge))
            .ToList();
    }

    public static List<Punkt> PunkterForDor(IEnumerable<string> typer, IReadOnlyList<ServiceKontrollpunkt> mal, int dorId, int? prosjektId = null)
    {
        var felles = FellesFor(mal, prosjektId);
        var liste = PunkterFor(typer, felles).Select(p =>
        {
            var kilder = p.Kilde.Split(" + ");
            var treff = felles.Where(m => m.Tekst.Equals(p.Tekst, StringComparison.OrdinalIgnoreCase) && kilder.Contains(m.Servicetype, StringComparer.OrdinalIgnoreCase)).ToList();
            return new Punkt(p.Tekst, p.Kilde, treff.Select(m => m.Standard).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)), treff.Any(m => m.KreverFoto));
        }).ToList();
        // Avtalens punkter for alle dører.
        if (prosjektId is not null)
        {
            foreach (var m in mal.Where(m => m.ProsjektId == prosjektId && !m.SkruddAv && m.DorId == null && m.Servicetype == AlleDorer).OrderBy(m => m.Rekkefolge))
            {
                if (!liste.Any(x => x.Tekst.Equals(m.Tekst, StringComparison.OrdinalIgnoreCase)))
                {
                    liste.Add(new Punkt(m.Tekst, AlleDorer, m.Standard, m.KreverFoto));
                }
            }
        }
        foreach (var m in mal.Where(m => m.DorId == dorId).OrderBy(m => m.Rekkefolge))
        {
            if (!liste.Any(x => x.Tekst.Equals(m.Tekst, StringComparison.OrdinalIgnoreCase)))
            {
                liste.Add(new Punkt(m.Tekst, "Denne døren", m.Standard, m.KreverFoto));
            }
        }
        return liste;
    }

    public sealed record KomponentKopi(string Navn, string? Produktkode, string? Komponenttype, string? Servicetype, bool Montert);

    public static List<KomponentKopi> LesKomponenter(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<KomponentKopi>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    public static Task<Servicerunde?> PagaendeRunde(ApplicationDbContext db, int prosjektId) =>
        db.Servicerunder.Where(r => r.ProsjektId == prosjektId && r.DorForDor && r.FullfortDato == null && !r.ErPlanlagt)
            .OrderByDescending(r => r.Id).FirstOrDefaultAsync();

    public sealed record Forhandsvisning(
        int Dorer, int Plantegninger, int ApneDriftsmeldinger, int ApneAvvik,
        IReadOnlyList<string> EndredeDorer, int? PagaendeRundeId, DateTime? ForrigeRunde);

    public static async Task<Forhandsvisning> ForhandsvisNyRunde(ApplicationDbContext db, int prosjektId)
    {
        var dorer = await db.Dorer.AsNoTracking().Where(d => d.ProsjektId == prosjektId).Select(d => new { d.Id, d.Dornummer, d.PlantegningId }).ToListAsync();
        var ider = dorer.Select(d => d.Id).ToList();
        var forrige = await db.Servicerunder.AsNoTracking()
            .Where(r => r.ProsjektId == prosjektId && !r.ErPlanlagt && (!r.DorForDor || r.FullfortDato != null))
            .OrderByDescending(r => r.Dato).Select(r => (DateTime?)r.Dato).FirstOrDefaultAsync();
        var endret = forrige is { } f
            ? await db.DorKomponentLogger.AsNoTracking().Where(l => ider.Contains(l.DorId) && l.Tidspunkt > f).Select(l => l.DorId).Distinct().ToListAsync()
            : [];
        var meldinger = await DriftHelper.HentMeldinger(db, db.Driftsmeldinger.Where(m => m.Dor!.ProsjektId == prosjektId));
        var avvik = await db.Avvik.CountAsync(a => ider.Contains(a.DorId) && a.Status != AvvikStatus.Godkjent && a.Status != AvvikStatus.Lukket);
        var pagar = await PagaendeRunde(db, prosjektId);
        return new Forhandsvisning(
            dorer.Count,
            dorer.Where(d => d.PlantegningId != null).Select(d => d.PlantegningId).Distinct().Count(),
            meldinger.Count(m => m.ErApen),
            avvik,
            dorer.Where(d => endret.Contains(d.Id)).Select(d => d.Dornummer).ToList(),
            pagar?.Id,
            forrige);
    }

    /// Oppretter runden med øyeblikksbilde av alle dørene. Finnes det allerede en
    /// pågående runde på avtalen, returneres den i stedet - det lages aldri to.
    public static async Task<int> OpprettRunde(ApplicationDbContext db, int prosjektId, int? brukerId, DateTime dato)
    {
        if (await PagaendeRunde(db, prosjektId) is { } finnes)
        {
            return finnes.Id;
        }

        await SikreKontrollpunkter(db);
        await ServiceRegelMotor.SikreStandardregler(db);
        var mal = await db.ServiceKontrollpunkter.AsNoTracking().ToListAsync();
        var regler = await ServiceRegelMotor.HentRegler(db);
        var typeKart = await ServiceRegelMotor.HentTypeKart(db);
        var typeNavn = await db.ComponentTypes.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Navn);
        var dorer = await db.Dorer.AsNoTracking().Where(d => d.ProsjektId == prosjektId)
            .Include(d => d.Komponenter).ThenInclude(k => k.Component)
            .Include(d => d.Funksjoner)
            .Include(d => d.Plantegning)
            .OrderBy(d => d.Etasje).ThenBy(d => d.Dornummer).ToListAsync();

        // En planlagt runde (fra planleggingen eller forrige fullførte runde) som
        // forfaller innen 60 dager, tas i bruk i stedet for å lage en ny.
        var planlagt = await db.Servicerunder
            .Where(r => r.ProsjektId == prosjektId && r.ErPlanlagt && r.Dato <= dato.Date.AddDays(60))
            .OrderBy(r => r.Dato).FirstOrDefaultAsync();
        var runde = planlagt ?? new Servicerunde { ProsjektId = prosjektId, StatusBeskrivelse = "Pågår" };
        runde.Dato = dato;
        runde.ErPlanlagt = false;
        runde.DorForDor = true;
        runde.StatusBeskrivelse = "Pågår";
        runde.UtfortAvBrukerId = brukerId ?? runde.UtfortAvBrukerId;
        var nr = 0;
        foreach (var d in dorer)
        {
            var res = ServiceRegelMotor.Klassifiser(d, regler, typeKart);
            var komp = d.Komponenter.Where(k => k.Component is not null).Select(k =>
            {
                var tid = k.Component!.ComponentTypeId;
                return new KomponentKopi(k.Component.Navn, k.Component.Produktkode,
                    tid is { } t1 ? typeNavn.GetValueOrDefault(t1) : null,
                    tid is { } t2 ? typeKart.GetValueOrDefault(t2) : null, k.Montert);
            }).ToList();
            var rd = new ServicerundeDor
            {
                DorId = d.Id,
                Dornummer = d.Dornummer,
                Rom = string.Join(" · ", new[] { d.Romnr, d.DorTil }.Where(s => !string.IsNullOrWhiteSpace(s))),
                Etasje = d.Etasje,
                PlantegningId = d.PlantegningId,
                PlantegningNavn = d.Plantegning?.Navn,
                PosX = d.PosX,
                PosY = d.PosY,
                Kategori = res.Regel?.Navn,
                KomponenterJson = JsonSerializer.Serialize(komp),
                Rekkefolge = nr++
            };
            var i = 0;
            foreach (var p in PunkterForDor(res.Typer, mal, d.Id, prosjektId))
            {
                rd.Punkter.Add(new ServicerundeDorPunkt { Tekst = p.Tekst, Kilde = p.Kilde, Standard = p.Standard, KreverFoto = p.KreverFoto, Rekkefolge = i++ });
            }
            runde.Dorer.Add(rd);
        }
        if (planlagt is null)
        {
            db.Servicerunder.Add(runde);
        }
        await db.SaveChangesAsync();
        return runde.Id;
    }

    /// Status på døren ut fra punktene: avvik hvis ett punkt har avvik, OK når
    /// alle punktene er satt, ellers ikke kontrollert.
    public static string BeregnStatus(ServicerundeDor d) =>
        d.Punkter.Any(p => p.Status == ServicerundeDorStatus.Avvik) ? ServicerundeDorStatus.Avvik
        : d.Punkter.Count > 0 && d.Punkter.All(p => p.Status is not null) ? ServicerundeDorStatus.Ok
        : ServicerundeDorStatus.IkkeKontrollert;

    /// Fullfører runden: låser den, skriver oppsummeringen, foreslår neste
    /// service ut fra frekvensen og gjør hver dør med avvik om til et vanlig
    /// avvik (AVV-nummer) som følges opp der. Kjøres den på nytt, lages ingen
    /// nye avvik for dører som allerede har fått det.
    public static async Task<List<Avvik>> Fullfor(ApplicationDbContext db, Servicerunde runde, int? brukerId)
    {
        var dorer = runde.Dorer;
        runde.FullfortDato ??= DateTime.Now;
        runde.StatusBeskrivelse = Oppsummering(dorer);
        var prosjekt = runde.Prosjekt ?? await db.Prosjekter.FindAsync(runde.ProsjektId);
        if (runde.NesteServiceDato is null && prosjekt?.ServiceGangerPerAr is > 0 and <= 12 && prosjekt.ServiceGangerPerAr is { } g)
        {
            runde.NesteServiceDato = runde.Dato.Date.AddMonths(12 / g);
        }

        var ider = dorer.Select(d => d.Id).ToList();
        var finnes = await db.Avvik.Where(a => a.ServicerundeDorId != null && ider.Contains(a.ServicerundeDorId.Value)).Select(a => a.ServicerundeDorId!.Value).ToListAsync();
        var nye = new List<Avvik>();
        foreach (var d in dorer.Where(d => d.Status == ServicerundeDorStatus.Avvik && !finnes.Contains(d.Id)))
        {
            var punkter = d.Punkter.Where(p => p.Status == ServicerundeDorStatus.Avvik).OrderBy(p => p.Rekkefolge).ToList();
            var linjer = punkter.Select(p => string.IsNullOrWhiteSpace(p.Kommentar) ? $"- {p.Tekst}" : $"- {p.Tekst}: {p.Kommentar}");
            var a = new Avvik
            {
                DorId = d.DorId,
                Tittel = $"Servicerunde {runde.Dato:dd.MM.yyyy}: {punkter.FirstOrDefault()?.Tekst ?? "avvik"}{(punkter.Count > 1 ? $" (+{punkter.Count - 1})" : "")}",
                Beskrivelse = string.Join("\n", linjer) + (string.IsNullOrWhiteSpace(d.Notat) ? "" : $"\n\nNotat: {d.Notat}"),
                OpprettetAvBrukerId = brukerId,
                ServicerundeDorId = d.Id
            };
            db.Avvik.Add(a);
            nye.Add(a);
        }
        // Neste runde planlegges automatisk - men bare hvis det ikke allerede
        // finnes en planlagt runde fram i tid.
        if (runde.NesteServiceDato is { } neste && !await db.Servicerunder.AnyAsync(r => r.ProsjektId == runde.ProsjektId && r.ErPlanlagt && r.Dato >= DateTime.Today))
        {
            db.Servicerunder.Add(new Servicerunde
            {
                ProsjektId = runde.ProsjektId,
                Dato = neste,
                ErPlanlagt = true,
                StatusBeskrivelse = "Planlagt servicerunde",
                UtfortAvBrukerId = runde.UtfortAvBrukerId
            });
        }
        await db.SaveChangesAsync();
        return nye;
    }

    public static string Oppsummering(IReadOnlyCollection<ServicerundeDor> dorer)
    {
        var ok = dorer.Count(d => d.Status == ServicerundeDorStatus.Ok);
        var avvik = dorer.Count(d => d.Status == ServicerundeDorStatus.Avvik);
        var ikke = dorer.Count - ok - avvik;
        return $"{ok + avvik} av {dorer.Count} dører kontrollert: {ok} uten anmerkning, {avvik} med avvik{(ikke > 0 ? $", {ikke} ikke kontrollert" : "")}.";
    }
}
