using System.Net;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Innkjøp i lagermodulen (fase 2): innkjøpsforslag, innkjøpsordre,
// ordrebekreftelse med avvikskontroll og varemottak. Selve
// beholdningsendringen ved mottak går gjennom LagerService, så snittkost og
// Lagerantall oppdateres på samme måte som for alle andre bevegelser.
// Fase 3: forslaget tar med det prosjekter venter på, og mottak på
// hovedlager fyller ventende reservasjoner (ReservasjonService).
public class InnkjopService(ApplicationDbContext db, LagerService lager, ReservasjonService reservasjoner, EmailService epost,
    InnkjopsordrePdfService pdf, ITenantContext tenantContext)
{
    // Ordre der varer fortsatt er på vei (teller som «bestilt»).
    public static readonly InnkjopStatus[] Apne =
        [InnkjopStatus.Sendt, InnkjopStatus.Bekreftet, InnkjopStatus.Avvik, InnkjopStatus.DelvisMottatt];

    // Ordre som kan tas imot.
    public static readonly InnkjopStatus[] KanMottas =
        [InnkjopStatus.Sendt, InnkjopStatus.Bekreftet, InnkjopStatus.DelvisMottatt];

    public sealed record Kilde(int? LeverandorId, string LeverandorNavn, string? Varenummer, string Navn, decimal Pris);

    // Minimum er null når forslaget bare skyldes prosjekter som venter.
    public sealed record Forslag(Component Vare, int PaLager, int Bestilt, int IUtkast, int? Minimum, int Venter, int Antall, Kilde Kilde);

    // Antall bestilt og ikke mottatt per vare (åpne ordre).
    public async Task<Dictionary<int, int>> BestiltAsync()
    {
        var linjer = await db.InnkjopsordreLinjer.AsNoTracking()
            .Where(l => Apne.Contains(l.Innkjopsordre!.Status))
            .Select(l => new { l.ComponentId, l.Antall, l.Mottatt })
            .ToListAsync();
        return linjer.GroupBy(l => l.ComponentId).ToDictionary(g => g.Key, g => g.Sum(l => Math.Max(0, l.Antall - l.Mottatt)));
    }

    // Hvor varen kjøpes: standardleverandøren på varen (ComponentLeverandor),
    // ellers leverandøren med samme navn som tekstfeltet Leverandor.
    public async Task<Dictionary<int, Kilde>> KilderAsync(IEnumerable<Component> varer)
    {
        var liste = varer.ToList();
        var ider = liste.Select(c => c.Id).ToList();
        var koblinger = await db.ComponentLeverandorer.AsNoTracking().Include(cl => cl.Leverandor)
            .Where(cl => ider.Contains(cl.ComponentId)).ToListAsync();
        var leverandorer = await db.Leverandorer.AsNoTracking().Select(l => new { l.Id, l.Navn }).ToListAsync();

        var resultat = new Dictionary<int, Kilde>();
        foreach (var c in liste)
        {
            var cl = koblinger.Where(k => k.ComponentId == c.Id).OrderByDescending(k => k.ErStandard).FirstOrDefault();
            if (cl?.Leverandor is { } lev)
            {
                resultat[c.Id] = new Kilde(lev.Id, lev.Navn, cl.Varenummer ?? c.Produktkode, c.Navn, cl.PrisNetto ?? c.PrisNetto ?? 0m);
                continue;
            }

            var navnTreff = leverandorer.FirstOrDefault(l => string.Equals(l.Navn.Trim(), c.Leverandor?.Trim(), StringComparison.OrdinalIgnoreCase));
            resultat[c.Id] = new Kilde(navnTreff?.Id, navnTreff?.Navn ?? (string.IsNullOrWhiteSpace(c.Leverandor) ? "Uten leverandør" : c.Leverandor.Trim()),
                c.Produktkode, c.Navn, c.PrisNetto ?? 0m);
        }

        return resultat;
    }

    // Tilgjengelig = på lager − reservert + bestilt + i utkast − det
    // prosjekter venter på. Forslag når tilgjengelig er på/under minimum
    // (fyller opp til 2 × minimum), eller under null for varer prosjekter
    // venter på (også varer som ikke er lagerført - de kjøpes inn til
    // prosjektet). Utkast regnes med, så ingenting foreslås to ganger.
    public async Task<List<Forslag>> ForslagAsync()
    {
        var venter = await reservasjoner.VenterAsync();
        var reservert = await reservasjoner.ReservertAsync();
        var venterIder = venter.Keys.ToList();
        var varer = await db.Components.AsNoTracking()
            .Where(c => c.Aktiv && ((c.ILagerstyring && c.Minimumsbeholdning != null) || venterIder.Contains(c.Id)))
            .ToListAsync();
        if (varer.Count == 0)
        {
            return [];
        }

        var beholdning = (await lager.BeholdningAsync()).GroupBy(b => b.ComponentId).ToDictionary(g => g.Key, g => g.Sum(b => b.Antall));
        var bestilt = await BestiltAsync();
        var utkast = (await db.InnkjopsordreLinjer.AsNoTracking()
                .Where(l => l.Innkjopsordre!.Status == InnkjopStatus.Utkast)
                .Select(l => new { l.ComponentId, l.Antall }).ToListAsync())
            .GroupBy(l => l.ComponentId).ToDictionary(g => g.Key, g => g.Sum(l => l.Antall));

        var aktuelle = new List<(Component Vare, int PaLager, int Bestilt, int Utkast, int? Min, int Venter, int Antall)>();
        foreach (var c in varer)
        {
            var paLager = beholdning.GetValueOrDefault(c.Id);
            var b = bestilt.GetValueOrDefault(c.Id);
            var u = utkast.GetValueOrDefault(c.Id);
            var v = venter.GetValueOrDefault(c.Id);
            var tilgjengelig = Math.Max(0, paLager) - reservert.GetValueOrDefault(c.Id) + b + u - v;
            if (c.ILagerstyring && c.Minimumsbeholdning is { } min)
            {
                if (tilgjengelig <= min)
                {
                    aktuelle.Add((c, paLager, b, u, min, v, Math.Max(1, Math.Max(min * 2, min + 1) - tilgjengelig)));
                }
            }
            else if (tilgjengelig < 0)
            {
                aktuelle.Add((c, paLager, b, u, null, v, -tilgjengelig));
            }
        }

        var kilder = await KilderAsync(aktuelle.Select(a => a.Vare));
        return aktuelle
            .Select(a => new Forslag(a.Vare, a.PaLager, a.Bestilt, a.Utkast, a.Min, a.Venter, a.Antall, kilder[a.Vare.Id]))
            .OrderBy(f => f.Kilde.LeverandorNavn).ThenBy(f => f.Vare.Navn)
            .ToList();
    }

    private async Task<int> NesteNummerAsync() =>
        (await db.Innkjopsordrer.MaxAsync(o => (int?)o.Nummer) ?? 1000) + 1;

    public async Task<Innkjopsordre> OpprettAsync(int? leverandorId, string leverandorNavn, string? opprettetAv,
        IEnumerable<(int ComponentId, int Antall)> linjer, int? prosjektId = null)
    {
        var lev = leverandorId is { } id ? await db.Leverandorer.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id) : null;
        var hoved = await lager.HovedlagerAsync();
        var ordre = new Innkjopsordre
        {
            Nummer = await NesteNummerAsync(),
            LeverandorId = lev?.Id,
            LeverandorNavn = lev?.Navn ?? leverandorNavn,
            LeverandorEpost = lev?.Epost,
            LagerId = hoved.Id,
            Status = InnkjopStatus.Utkast,
            OpprettetAv = opprettetAv,
            VarReferanse = opprettetAv,
            ProsjektId = prosjektId
        };

        var liste = linjer.Where(l => l.Antall > 0).ToList();
        if (liste.Count > 0)
        {
            var ider = liste.Select(l => l.ComponentId).ToList();
            var varer = await db.Components.AsNoTracking().Where(c => ider.Contains(c.Id)).ToListAsync();
            var kilder = await KilderAsync(varer);
            foreach (var (componentId, antall) in liste)
            {
                var vare = varer.First(c => c.Id == componentId);
                ordre.Linjer.Add(NyLinje(vare, kilder[componentId], antall));
            }
        }

        db.Innkjopsordrer.Add(ordre);
        await db.SaveChangesAsync();
        return ordre;
    }

    public async Task LeggTilLinjeAsync(Innkjopsordre ordre, int componentId, int antall)
    {
        var eksisterende = ordre.Linjer.FirstOrDefault(l => l.ComponentId == componentId);
        if (eksisterende is not null)
        {
            eksisterende.Antall += antall;
        }
        else
        {
            var vare = await db.Components.AsNoTracking().FirstAsync(c => c.Id == componentId);
            var kilde = (await KilderAsync([vare]))[componentId];
            ordre.Linjer.Add(NyLinje(vare, kilde, antall));
        }

        await db.SaveChangesAsync();
    }

    private static InnkjopsordreLinje NyLinje(Component vare, Kilde kilde, int antall) => new()
    {
        ComponentId = vare.Id,
        Navn = kilde.Navn,
        Varenummer = kilde.Varenummer,
        Enhet = vare.Enhet,
        Antall = antall,
        Pris = kilde.Pris
    };

    // Sender ordren som PDF på e-post. Svar fra leverandøren går til
    // organisasjonens e-post (eller den som sendte).
    public async Task<(bool Ok, string Melding)> SendAsync(Innkjopsordre ordre, string til, string? avsenderNavn, string? avsenderEpost)
    {
        if (ordre.Linjer.Count == 0)
        {
            return (false, "Ordren har ingen varelinjer.");
        }

        var fil = await pdf.GenerateAsync(ordre.Id);
        if (fil is null)
        {
            return (false, "Klarte ikke å lage PDF.");
        }

        var org = RapportProfil.Fra(tenantContext.Current);
        var svarTil = org.Epost ?? avsenderEpost;
        Func<string?, string?> e = WebUtility.HtmlEncode;
        var html = $"<p>Hei,</p><p>Vedlagt er innkjøpsordre <strong>{ordre.Visningsnummer}</strong> fra {e(org.Navn)}"
            + (ordre.OnsketLevering is { } d ? $", med ønsket levering <strong>{d:dd.MM.yyyy}</strong>" : "")
            + ".</p><p>Vennligst send ordrebekreftelse med pris og leveringsdato"
            + (svarTil is null ? "" : $" til {e(svarTil)}")
            + $", og merk pakkseddel og faktura med {ordre.Visningsnummer}.</p>"
            + $"<p>Med vennlig hilsen<br>{e(avsenderNavn ?? org.Navn)}<br>{e(org.Navn)}"
            + (org.Telefon is null ? "" : $"<br>Tlf {e(org.Telefon)}") + "</p>";

        var ok = await epost.SendAsync(til.Trim(), $"Innkjøpsordre {ordre.Visningsnummer} - {org.Navn}", html,
            [($"Innkjopsordre-{ordre.Visningsnummer}.pdf", fil)], svarTil);
        if (!ok)
        {
            return (false, "E-posten kunne ikke sendes. Last ned PDF-en og send den selv, og trykk «Marker som bestilt».");
        }

        MarkerSendt(ordre, til.Trim());
        await db.SaveChangesAsync();
        return (true, $"{ordre.Visningsnummer} er sendt til {til.Trim()}.");
    }

    public static void MarkerSendt(Innkjopsordre ordre, string? sendtTil)
    {
        ordre.Status = InnkjopStatus.Sendt;
        ordre.Sendt = DateTime.Now;
        ordre.SendtTil = sendtTil;
    }

    // Hvilke linjer i ordrebekreftelsen som avviker fra bestillingen.
    public static bool LinjeHarAvvik(Innkjopsordre ordre, InnkjopsordreLinje l) =>
        (l.BekreftetAntall is { } a && a != l.Antall)
        || (l.BekreftetPris is { } p && p != l.Pris)
        || (l.BekreftetLevering is { } d && ordre.OnsketLevering is { } onsket && d.Date > onsket.Date);

    // Lagrer ordrebekreftelsen. Uten avvik blir ordren Bekreftet, med avvik
    // må den godkjennes først.
    public async Task<bool> RegistrerBekreftelseAsync(Innkjopsordre ordre, string? brukerNavn)
    {
        foreach (var l in ordre.Linjer)
        {
            l.BekreftetAntall ??= l.Antall;
            l.BekreftetPris ??= l.Pris;
        }

        ordre.BekreftetDato = DateTime.Now;
        ordre.BekreftetAv = brukerNavn;
        var avvik = ordre.Linjer.Any(l => LinjeHarAvvik(ordre, l));
        if (avvik)
        {
            ordre.HaddeAvvik = true;
            ordre.AvvikPris = ordre.Linjer.Sum(l => (l.BekreftetAntall ?? l.Antall) * ((l.BekreftetPris ?? l.Pris) - l.Pris));
        }

        ordre.Status = avvik ? InnkjopStatus.Avvik : (ordre.Linjer.Any(l => l.Mottatt > 0) ? InnkjopStatus.DelvisMottatt : InnkjopStatus.Bekreftet);
        await db.SaveChangesAsync();
        return avvik;
    }

    // Godtar avvikene: bekreftet antall og pris blir gjeldende på ordren.
    public async Task GodkjennAvvikAsync(Innkjopsordre ordre, string? brukerNavn)
    {
        foreach (var l in ordre.Linjer)
        {
            if (l.BekreftetAntall is { } a)
            {
                l.Antall = Math.Max(a, l.Mottatt);
            }

            if (l.BekreftetPris is { } p)
            {
                l.Pris = p;
            }
        }

        ordre.BekreftetAv = brukerNavn;
        ordre.Status = ordre.Linjer.Any(l => l.Mottatt > 0) ? InnkjopStatus.DelvisMottatt : InnkjopStatus.Bekreftet;
        await db.SaveChangesAsync();
    }

    // Varemottak: antall per linje inn på ordrens lager, til ordrens innpris.
    // Det som ikke kommer, blir stående som restordre.
    public sealed record Mottaksresultat(int Totalt, List<(string Til, string Vare, int Antall)> TilReservasjoner);

    public async Task<Mottaksresultat> MottaAsync(Innkjopsordre ordre, IReadOnlyDictionary<int, int> antallPerLinje, string? notat,
        int? brukerId, string? brukerNavn)
    {
        var totalt = 0;
        var tilReservasjoner = new List<(string, string, int)>();
        var mottakslager = await db.Lagre.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ordre.LagerId);
        var tekst = $"Mottak {ordre.Visningsnummer}" + (string.IsNullOrWhiteSpace(notat) ? "" : $" · {notat.Trim()}");
        foreach (var l in ordre.Linjer)
        {
            var antall = antallPerLinje.GetValueOrDefault(l.Id);
            if (antall <= 0)
            {
                continue;
            }

            await lager.RegistrerAsync(l.ComponentId, ordre.LagerId, antall, LagerBevegelseType.Mottak, tekst,
                brukerId, brukerNavn, l.Pris, innkjopsordreId: ordre.Id);
            l.Mottatt += antall;
            totalt += antall;

            // Varer inn på hovedlager fyller reservasjoner som venter.
            if (mottakslager?.Type == LagerType.Hovedlager)
            {
                foreach (var (r, n) in await reservasjoner.FordelMottakAsync(l.ComponentId, antall, ordre.ProsjektId))
                {
                    tilReservasjoner.Add((r.Prosjekt?.Navn ?? r.Arbeidsordre?.Tittel ?? "Reservasjon", l.Navn, n));
                }
            }
        }

        if (totalt > 0)
        {
            ordre.Status = ordre.Linjer.All(l => l.Rest == 0) ? InnkjopStatus.Mottatt : InnkjopStatus.DelvisMottatt;
            await db.SaveChangesAsync();
        }

        return new Mottaksresultat(totalt, tilReservasjoner);
    }
}
