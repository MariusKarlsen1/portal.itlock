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
            foreach (var p in mal.Where(m => m.Servicetype.Equals(t, StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Rekkefolge))
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

        var runde = new Servicerunde
        {
            ProsjektId = prosjektId,
            Dato = dato,
            UtfortAvBrukerId = brukerId,
            StatusBeskrivelse = "Pågår",
            DorForDor = true
        };
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
            foreach (var (tekst, kilde) in PunkterFor(res.Typer, mal))
            {
                rd.Punkter.Add(new ServicerundeDorPunkt { Tekst = tekst, Kilde = kilde, Rekkefolge = i++ });
            }
            runde.Dorer.Add(rd);
        }
        db.Servicerunder.Add(runde);
        await db.SaveChangesAsync();
        return runde.Id;
    }

    /// Status på døren ut fra punktene: avvik hvis ett punkt har avvik, OK når
    /// alle punktene er satt, ellers ikke kontrollert.
    public static string BeregnStatus(ServicerundeDor d) =>
        d.Punkter.Any(p => p.Status == ServicerundeDorStatus.Avvik) ? ServicerundeDorStatus.Avvik
        : d.Punkter.Count > 0 && d.Punkter.All(p => p.Status is not null) ? ServicerundeDorStatus.Ok
        : ServicerundeDorStatus.IkkeKontrollert;

    public static string Oppsummering(IReadOnlyCollection<ServicerundeDor> dorer)
    {
        var ok = dorer.Count(d => d.Status == ServicerundeDorStatus.Ok);
        var avvik = dorer.Count(d => d.Status == ServicerundeDorStatus.Avvik);
        var ikke = dorer.Count - ok - avvik;
        return $"{ok + avvik} av {dorer.Count} dører kontrollert: {ok} uten anmerkning, {avvik} med avvik{(ikke > 0 ? $", {ikke} ikke kontrollert" : "")}.";
    }
}
