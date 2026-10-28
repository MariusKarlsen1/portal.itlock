using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Prosjektøkonomi: tilbudt mot faktisk, prognose og status.
//
// Budsjett  = godkjente tilbud og endringsmeldinger (låst i TilbudBudsjett
//             første gang de regnes ut etter godkjenning).
// Faktisk   = timer ført på prosjektets arbeidsordrer × timekost, pluss
//             varer: lagerforbruk på prosjektet når lagermodulen er i bruk,
//             ellers anslått fra tilbudets varer × fremdrift. Varer lagt
//             direkte på arbeidsordrene (som ikke er lagerført) kommer i tillegg.
// Fremdrift = dører ferdig montert av alle dører (ellers ferdige arbeidsordre).
// Prognose  = det faktiske skalert opp til 100 % fremdrift.
//
// Bare prosjekter med minst ett godkjent tilbud («vunnet») følges opp.
public class ProsjektOkonomiService(ApplicationDbContext db, TilleggsmodulService tilleggsmoduler)
{
    // Pågående prosjekter: registrert (vunnet) eller under arbeid.
    private static readonly ProsjektStatus[] Pagaende = [ProsjektStatus.Registrert, ProsjektStatus.UnderArbeid];

    public enum OkonomiStatus { IkkeVunnet, IRute, FolgMed, OverBudsjett }

    public sealed record TilbudInfo(int Id, string Tittel, bool ErEndring, decimal Salgssum, decimal Varekost, decimal Timer, DateTime Laast, decimal AndreKost = 0,
        decimal SalgVarer = 0, decimal SalgArbeid = 0, decimal SalgAndre = 0, decimal Timekost = 0)
    {
        public decimal Kost => Varekost + Timekost + AndreKost;
    }

    public sealed record AoLinje(int Id, string Tittel, ArbeidsordreStatus Status, decimal Timer, decimal TimerVenter, decimal Timekost, decimal Varer, decimal? IkkeFakturert);

    public sealed record Okonomi(
        int ProsjektId,
        OkonomiStatus Status,
        string StatusTekst,
        decimal Inntekt,
        decimal BudsjettTimer,
        decimal BudsjettTimekost,
        decimal BudsjettVarer,
        decimal FaktiskTimer,
        decimal FaktiskTimerVenter,
        decimal FaktiskTimekost,
        decimal FaktiskVarer,
        bool VarerFraLager,
        decimal Fremdrift,
        string FremdriftTekst,
        decimal PrognoseTimer,
        decimal PrognoseVarer,
        decimal Timesats,
        List<TilbudInfo> Tilbud,
        List<AoLinje> Arbeidsordre,
        decimal Justering = 0,
        bool RestOverstyrt = false,
        bool VarerManuell = false,
        decimal BudsjettAndre = 0,
        decimal BudsjettAndreProsent = 0,
        decimal? FaktiskAndreRegistrert = null,
        decimal JusteringVarer = 0,
        decimal JusteringTimer = 0,
        decimal JusteringUforutsett = 0)
    {
        // Det kunden betaler for hver post, summert fra de godkjente tilbudene.
        public decimal SalgVarer => Tilbud.Sum(t => t.SalgVarer);
        public decimal SalgArbeid => Tilbud.Sum(t => t.SalgArbeid);
        public decimal SalgAndre => Tilbud.Sum(t => t.SalgAndre);

        // Fortjenesten slik den ble tilbudt.
        public decimal TilbudtFortjeneste => Inntekt - BudsjettKost;

        // Rammen per post: kosten i tilbudet pluss justeringene underveis.
        public decimal RammeVarer => BudsjettVarer + JusteringVarer;
        public decimal RammeTimer => BudsjettTimekost + JusteringTimer;
        public decimal RammeAndre => BudsjettAndre;

        // Det som allerede er brukt mer enn rammen. Ingen prognose: bare
        // faktisk brukt mot det som er tilbudt.
        public decimal OverVarer => Math.Max(0, FaktiskVarer - RammeVarer);
        public decimal OverTimer => Math.Max(0, FaktiskTimekost - RammeTimer);
        public decimal OverAndre => Math.Max(0, FaktiskAndre - RammeAndre);

        // Fortjenesten som er igjen: inntekten minus kosten per post, der en
        // post teller med det som er størst av rammen og det som er brukt.
        public decimal FortjenesteIgjen => Inntekt - Math.Max(RammeTimer, FaktiskTimekost) - Math.Max(RammeVarer, FaktiskVarer)
            - Math.Max(RammeAndre, FaktiskAndre) - JusteringUforutsett;

        // Mer enn 5 % av fortjenesten i tilbudet er spist opp, eller prosjektet går med tap.
        public bool TaperFortjeneste => Inntekt > 0 && (FortjenesteIgjen < 0 || TilbudtFortjeneste - FortjenesteIgjen > Math.Max(0, TilbudtFortjeneste) * 0.05m);

        // Varer og kjøring er anslått (ikke registrert) når ingenting er tatt
        // ut fra lageret, lagt inn for hånd eller ført på arbeidsordrene.
        public bool VarerAnslatt => !VarerFraLager && !VarerManuell && BudsjettVarer > 0;
        public bool AndreAnslatt => !AndreRegistrert && BudsjettAndre > 0;

        // Kjøring, småmateriell og tillegg fra kodelinjene. Det montørene
        // fører på arbeidsordrene er faktisk kost;
        // prosentlinjene (og alt før noe er ført) anslås etter fremdriften.
        public bool AndreRegistrert => FaktiskAndreRegistrert is not null;
        public decimal FaktiskAndre => FaktiskAndreRegistrert is { } r
            ? Math.Round(r + BudsjettAndreProsent * Fremdrift, 2)
            : Math.Round(BudsjettAndre * Fremdrift, 2);
        public decimal PrognoseAndre
        {
            get
            {
                if (FaktiskAndreRegistrert is not { } r) return BudsjettAndre;
                var reg = Fremdrift >= 0.999m ? r
                    : Fremdrift >= 0.1m ? Math.Max(r, r / Fremdrift)
                    : Math.Max(r, BudsjettAndre - BudsjettAndreProsent);
                return Math.Round(reg + BudsjettAndreProsent, 2);
            }
        }
        public decimal BudsjettKost => BudsjettTimekost + BudsjettVarer + BudsjettAndre;
        // Budsjettet med justeringene som er lagt inn underveis.
        public decimal JustertKost => BudsjettKost + Justering;
        public decimal FaktiskKost => FaktiskTimekost + FaktiskVarer + FaktiskAndre;
        public decimal PrognoseKost => PrognoseTimekost + PrognoseVarer + PrognoseAndre;
        public decimal PrognoseTimekost => FaktiskTimer > 0 ? PrognoseTimer * (FaktiskTimekost / FaktiskTimer) : PrognoseTimer * Timesats;
        public bool ManglerTimesats => Timesats <= 0 && FaktiskTimekost <= 0;
        public decimal? BudsjettDekningsgrad => Inntekt > 0 ? (Inntekt - BudsjettKost) / Inntekt : null;
        public decimal? FaktiskDekningsgrad => Inntekt > 0 ? (Inntekt - FaktiskKost) / Inntekt : null;
        public decimal? PrognoseDekningsgrad => Inntekt > 0 ? (Inntekt - PrognoseKost) / Inntekt : null;
        public decimal IkkeFakturert => Arbeidsordre.Sum(a => a.IkkeFakturert ?? 0);
        public int AntallIkkeFakturert => Arbeidsordre.Count(a => a.IkkeFakturert is not null);
    }

    public async Task<OkonomiInnstillinger> InnstillingerAsync()
    {
        var inn = await db.OkonomiInnstillinger.OrderBy(i => i.Id).FirstOrDefaultAsync();
        if (inn is null)
        {
            inn = new OkonomiInnstillinger();
            db.OkonomiInnstillinger.Add(inn);
            await db.SaveChangesAsync();
        }

        return inn;
    }

    public async Task<Okonomi> BeregnAsync(int prosjektId) => (await BeregnFlereAsync([prosjektId]))[prosjektId];

    // Prosjekter som følges opp nå: pågående og med godkjent tilbud.
    public async Task<List<int>> AktiveVunneIderAsync() =>
        await db.Prosjekter.AsNoTracking()
            .Where(p => p.Status != null && Pagaende.Contains(p.Status.Value))
            .Where(p => db.Tilbud.Any(t => t.ProsjektId == p.Id && t.Status == EndringsmeldingStatus.Godkjent))
            .Select(p => p.Id).ToListAsync();

    public async Task<Dictionary<int, string>> ProsjektnavnAsync(IEnumerable<int> ider)
    {
        var liste = ider.ToList();
        return await db.Prosjekter.AsNoTracking().Where(p => liste.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Navn);
    }

    // Overleverte prosjekter som ikke er kontrollert ennå.
    public async Task<List<Prosjekt>> TilGjennomgangAsync() =>
        await db.Prosjekter.AsNoTracking().Include(p => p.Kunde)
            .Where(p => p.Status == ProsjektStatus.Overlevert)
            .Where(p => !db.ProsjektEtterkalkyler.Any(k => k.ProsjektId == p.Id && !k.ErUtkast))
            .OrderBy(p => p.OverlevertDato ?? p.OpprettetDato)
            .ToListAsync();

    public async Task<Dictionary<int, Okonomi>> BeregnFlereAsync(IReadOnlyCollection<int> prosjektIder)
    {
        var ider = prosjektIder.Distinct().ToList();
        var inn = await InnstillingerAsync();
        var harLager = await tilleggsmoduler.HarAsync(TilleggsModuler.Lager);

        // ---- Budsjett: godkjente tilbud, låst ved første utregning ----
        var tilbud = await db.Tilbud.Include(t => t.Linjer).Include(t => t.KodeLinjer)
            .Where(t => ider.Contains(t.ProsjektId) && t.Status == EndringsmeldingStatus.Godkjent)
            .ToListAsync();
        var tilbudIder = tilbud.Select(t => t.Id).ToList();
        var budsjetter = await db.TilbudBudsjetter.Where(b => tilbudIder.Contains(b.TilbudId)).ToDictionaryAsync(b => b.TilbudId);
        var nye = false;
        foreach (var t in tilbud.Where(t => !budsjetter.ContainsKey(t.Id)))
        {
            var minutter = t.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering).Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
            var estTimer = (t.EstimertTimerOverride ?? minutter / 60m) * (1 + (t.RiggDriftProsent ?? 0) / 100m);
            var b = new TilbudBudsjett
            {
                TilbudId = t.Id,
                Salgssum = Math.Round(TilbudBeregningHelper.BeregnTotaltUtenMva(t), 2),
                Varekost = Math.Round(t.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering)
                    .Sum(l => l.Innpris * (1 - l.EkstraRabattInnProsent / 100m) * l.Antall), 2),
                Timer = Math.Round(estTimer, 2),
                Timesats = t.MontasjeInnpris ?? inn.InternTimekost ?? 0m
            };

            // Kodelinjer: timene og timekosten fra arbeidslinjene (time), og
            // kjøring, småmateriell og tillegg som egen post.
            if (t.BrukerKodelinjer && t.KodeLinjer.Count > 0)
            {
                var utprisVarer = t.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering).Sum(l => l.Utpris * l.Antall);
                var summer = t.KodeLinjer.Select(l => (Linje: l, Sum: KodeLinjeBeregner.Sum(l, t.KodeLinjer, utprisVarer))).ToList();
                var arbeid = summer.Where(x => x.Linje.Type == PrisKodeType.Arbeid && x.Linje.Enhet == PrisKodeEnhet.Time).ToList();
                var arbeidTimer = arbeid.Sum(x => x.Linje.Antall);
                b.Timer = Math.Round(arbeidTimer, 2);
                b.Timesats = arbeidTimer > 0 ? Math.Round(arbeid.Sum(x => x.Sum.Kost) / arbeidTimer, 2) : inn.InternTimekost ?? 0m;
                b.AndreKost = Math.Round(summer.Except(arbeid).Sum(x => x.Sum.Kost), 2);
                b.AndreProsentKost = Math.Round(summer.Except(arbeid).Where(x => x.Linje.Enhet.ErProsent()).Sum(x => x.Sum.Kost), 2);
            }
            Fordel(b, t);
            db.TilbudBudsjetter.Add(b);
            budsjetter[t.Id] = b;
            nye = true;
        }

        // Budsjetter låst før salget ble fordelt på postene, fylles ut nå.
        foreach (var t in tilbud.Where(t => budsjetter[t.Id].SalgVarer is null))
        {
            Fordel(budsjetter[t.Id], t);
            nye = true;
        }

        if (nye)
        {
            await db.SaveChangesAsync();
        }

        // ---- Faktisk: arbeidsordre, timer, varer, lager ----
        var ordre = await db.Arbeidsordre.AsNoTracking()
            .Where(a => a.ProsjektId != null && ider.Contains(a.ProsjektId.Value))
            .Select(a => new { a.Id, a.Tittel, a.Status, ProsjektId = a.ProsjektId!.Value, a.FakturertDato, a.KostprisTime, a.Timepris, a.EstimerteTimer, a.TilbudId })
            .ToListAsync();
        var ordreIder = ordre.Select(o => o.Id).ToList();
        var timer = (await db.Timeregistreringer.AsNoTracking()
                .Where(t => ordreIder.Contains(t.ArbeidsordreId) && t.Status != TimeregistreringStatus.Avslatt)
                .Select(t => new { t.ArbeidsordreId, t.MontorId, t.Start, t.Slutt, t.PauseMinutter, t.Status }).ToListAsync())
            .Select(t => new { t.ArbeidsordreId, t.MontorId, t.Status, Timer = Math.Max(0, (decimal)(t.Slutt - t.Start).TotalHours - t.PauseMinutter / 60m) })
            .ToList();
        // Timekost per ansatt (Innstillinger → Timekost) går foran den felles timekosten.
        var ansattKost = (await db.AnsattTimekost.AsNoTracking().ToListAsync())
            .Where(k => k.Timekost > 0).ToDictionary(k => k.BrukerId, k => k.Timekost);
        var justeringer = await db.ProsjektBudsjettJusteringer.AsNoTracking().Where(j => ider.Contains(j.ProsjektId)).ToListAsync();
        var manuelleVarer = await db.Prosjekter.AsNoTracking().Where(p => ider.Contains(p.Id) && p.FaktiskVarekost != null)
            .ToDictionaryAsync(p => p.Id, p => p.FaktiskVarekost!.Value);
        var rester = await db.Prosjekter.AsNoTracking().Where(p => ider.Contains(p.Id) && p.ResterendeTimer != null)
            .ToDictionaryAsync(p => p.Id, p => p.ResterendeTimer!.Value);
        var aoVarer = await db.ArbeidsordreVarer.AsNoTracking()
            .Where(v => ordreIder.Contains(v.ArbeidsordreId))
            .Select(v => new { v.ArbeidsordreId, v.ComponentId, v.Antall, v.Kostpris, v.Utpris, Lagerfort = v.Component != null && v.Component.ILagerstyring })
            .ToListAsync();
        var lagerForbruk = harLager
            ? (await db.LagerBevegelser.AsNoTracking()
                .Where(b => b.Type == LagerBevegelseType.Forbruk && b.ProsjektId != null && ider.Contains(b.ProsjektId.Value))
                .Select(b => new { ProsjektId = b.ProsjektId!.Value, b.ArbeidsordreId, b.Antall, b.KostPerStk }).ToListAsync())
            : [];

        // Fremdrift fra dørene.
        var dorer = await db.Dorer.AsNoTracking().Where(d => ider.Contains(d.ProsjektId))
            .GroupBy(d => d.ProsjektId)
            .Select(g => new { ProsjektId = g.Key, Alle = g.Count(), Ferdig = g.Count(d => d.Status == MontasjeStatus.FerdigMontert) })
            .ToListAsync();

        // Overleverte og avsluttede prosjekter regnes som ferdige.
        var ferdigeProsjekter = (await db.Prosjekter.AsNoTracking()
            .Where(p => ider.Contains(p.Id) && (p.Status == ProsjektStatus.Overlevert || p.Status == ProsjektStatus.Avsluttet))
            .Select(p => p.Id).ToListAsync()).ToHashSet();

        var koder = await db.PrisKoder.AsNoTracking().ToListAsync();
        var aoKoder = await db.ArbeidsordreKodeLinjer.AsNoTracking().Where(l => ordreIder.Contains(l.ArbeidsordreId)).ToListAsync();

        // Beløp som ikke er fakturert: ferdigmeldte ordre som ikke er fakturert.
        var ikkeFakturertIder = ordre.Where(o => o.Status == ArbeidsordreStatus.Ferdig && o.FakturertDato == null).Select(o => o.Id).ToList();
        var ikkeFakturert = ikkeFakturertIder.Count == 0
            ? new Dictionary<int, decimal>()
            : (await db.Arbeidsordre.AsNoTracking().Include(a => a.Tilbud!).ThenInclude(t => t.Linjer).Include(a => a.Varer)
                    .Include(a => a.Tilbud!).ThenInclude(t => t.KodeLinjer).Include(a => a.KodeLinjer).Include(a => a.Timeregistreringer)
                    .Where(a => ikkeFakturertIder.Contains(a.Id)).ToListAsync())
                .ToDictionary(a => a.Id, a => ArbeidsordreOkonomiBeregner.BeregnLinjer(a, koder).Sum(l => l.Belop));

        var resultat = new Dictionary<int, Okonomi>();
        foreach (var pid in ider)
        {
            var pTilbud = tilbud.Where(t => t.ProsjektId == pid).OrderBy(t => t.Id)
                .Select(t => { var b = budsjetter[t.Id]; return new TilbudInfo(t.Id, t.Tittel, t.Type == TilbudType.Endringsmelding, b.Salgssum, b.Varekost, b.Timer, b.Laast, b.AndreKost,
                    b.SalgVarer ?? 0, b.SalgArbeid ?? 0, b.SalgAndre ?? 0, Math.Round(b.Timer * b.Timesats, 2)); })
                .ToList();
            var pBudsjett = tilbud.Where(t => t.ProsjektId == pid).Select(t => budsjetter[t.Id]).ToList();
            var budTimer = pBudsjett.Sum(b => b.Timer);
            var budSats = budTimer > 0 ? pBudsjett.Sum(b => b.Timer * b.Timesats) / budTimer : pBudsjett.Select(b => b.Timesats).FirstOrDefault();
            var timesats = inn.InternTimekost ?? (budSats > 0 ? budSats : 0m);
            var budVarer = pBudsjett.Sum(b => b.Varekost);
            var inntekt = pBudsjett.Sum(b => b.Salgssum);

            var pOrdre = ordre.Where(o => o.ProsjektId == pid).ToList();
            var aoLinjer = pOrdre.Select(o =>
            {
                var t = timer.Where(x => x.ArbeidsordreId == o.Id).ToList();
                var tSum = t.Sum(x => x.Timer);
                var tKost = t.Sum(x => x.Timer * (o.KostprisTime ?? ansattKost.GetValueOrDefault(x.MontorId, timesats)));
                var varer = aoVarer.Where(v => v.ArbeidsordreId == o.Id && !(harLager && v.Lagerfort)).Sum(v => v.Kostpris * v.Antall)
                    + (harLager ? -lagerForbruk.Where(f => f.ArbeidsordreId == o.Id).Sum(f => f.Antall * f.KostPerStk) : 0m);
                return new AoLinje(o.Id, o.Tittel, o.Status, Math.Round(tSum, 2), Math.Round(t.Where(x => x.Status == TimeregistreringStatus.Venter).Sum(x => x.Timer), 2),
                    Math.Round(tKost, 2), Math.Round(varer, 2), ikkeFakturert.TryGetValue(o.Id, out var ikke) ? ikke : null);
            }).ToList();

            var fTimer = aoLinjer.Sum(a => a.Timer);
            var fTimekost = aoLinjer.Sum(a => a.Timekost);
            var effSats = fTimer > 0 && fTimekost > 0 ? fTimekost / fTimer : timesats;
            var budTimekost = pBudsjett.Sum(b => b.Timer * (b.Timesats > 0 ? b.Timesats : effSats));
            if (timesats <= 0) timesats = effSats;

            // Tilbud uten egen timesats: timekosten regnes som i budsjettet over.
            pTilbud = pTilbud.Select(x => x.Timekost == 0 && x.Timer > 0 ? x with { Timekost = Math.Round(x.Timer * effSats, 2) } : x).ToList();

            // Kjøring og tillegg montørene har ført på arbeidsordrene. Uten noe
            // ført brukes anslaget fra tilbudet.
            var pOrdreIder = pOrdre.Select(o => o.Id).ToHashSet();
            var pKoder = aoKoder.Where(l => pOrdreIder.Contains(l.ArbeidsordreId)).ToList();
            decimal? andreRegistrert = pKoder.Count > 0 ? ArbeidsordreOkonomiBeregner.AndreKost(pKoder) : null;

            // Fremdrift
            var d = dorer.FirstOrDefault(x => x.ProsjektId == pid);
            decimal fremdrift;
            string fremdriftTekst;
            if (ferdigeProsjekter.Contains(pid))
            {
                fremdrift = 1;
                fremdriftTekst = "Prosjektet er overlevert";
            }
            else if (d is { Alle: > 0 })
            {
                fremdrift = (decimal)d.Ferdig / d.Alle;
                fremdriftTekst = $"{d.Ferdig} av {d.Alle} dører ferdig montert";
            }
            else if (pOrdre.Count > 0)
            {
                var ferdige = pOrdre.Count(o => o.Status is ArbeidsordreStatus.Ferdig or ArbeidsordreStatus.Fakturert);
                fremdrift = (decimal)ferdige / pOrdre.Count;
                fremdriftTekst = $"{ferdige} av {pOrdre.Count} arbeidsordre ferdig";
            }
            else
            {
                fremdrift = 0;
                fremdriftTekst = "Ingen dører eller arbeidsordre ennå";
            }

            // Varer: lagerforbruk (faktisk) eller anslag fra tilbudet × fremdrift.
            var direkteVarer = aoVarer.Where(v => pOrdre.Any(o => o.Id == v.ArbeidsordreId) && !(harLager && v.Lagerfort)).Sum(v => v.Kostpris * v.Antall);
            var fraLager = harLager && lagerForbruk.Any(f => f.ProsjektId == pid);
            decimal fVarer;
            if (fraLager)
            {
                fVarer = -lagerForbruk.Where(f => f.ProsjektId == pid).Sum(f => f.Antall * f.KostPerStk) + direkteVarer;
            }
            else
            {
                fVarer = budVarer * fremdrift + direkteVarer;
            }

            // Prognose: det faktiske skalert opp til ferdig prosjekt.
            decimal progTimer, progVarer;
            if (fremdrift >= 0.999m)
            {
                progTimer = fTimer;
                progVarer = fVarer;
            }
            else if (fremdrift >= 0.1m)
            {
                progTimer = Math.Max(fTimer, fTimer / fremdrift);
                // Varer som ikke er tatt ut fra lageret ennå, antas å bli brukt som tilbudt.
                progVarer = fraLager ? Math.Max(budVarer + direkteVarer, (fVarer - direkteVarer) / fremdrift + direkteVarer) : budVarer + direkteVarer;
            }
            else
            {
                progTimer = Math.Max(budTimer, fTimer);
                progVarer = Math.Max(budVarer + direkteVarer, fVarer);
            }

            // Prosjektlederen har lagt inn hvor mange timer som gjenstår.
            var restOverstyrt = fremdrift < 0.999m && rester.ContainsKey(pid);
            if (restOverstyrt)
            {
                progTimer = fTimer + Math.Max(0, rester[pid]);
            }

            // Faktisk varekost lagt inn for hånd går foran anslaget fra tilbudet.
            var varerManuell = !fraLager && manuelleVarer.ContainsKey(pid);
            if (varerManuell)
            {
                fVarer = manuelleVarer[pid];
                progVarer = fremdrift >= 0.999m ? fVarer : Math.Max(fVarer, budVarer + direkteVarer);
            }

            var pJust = justeringer.Where(j => j.ProsjektId == pid).ToList();
            var o2 = new Okonomi(pid, OkonomiStatus.IkkeVunnet, "", inntekt, Math.Round(budTimer, 2), Math.Round(budTimekost, 2), Math.Round(budVarer, 2),
                Math.Round(fTimer, 2), aoLinjer.Sum(a => a.TimerVenter), Math.Round(fTimekost, 2), Math.Round(fVarer, 2), fraLager,
                fremdrift, fremdriftTekst, Math.Round(progTimer, 2), Math.Round(progVarer, 2), Math.Round(timesats, 2), pTilbud, aoLinjer,
                pJust.Sum(j => j.Belop), restOverstyrt, varerManuell, Math.Round(pBudsjett.Sum(b => b.AndreKost), 2),
                Math.Round(pBudsjett.Sum(b => b.AndreProsentKost), 2), andreRegistrert,
                pJust.Where(j => j.Post == BudsjettPost.Varer).Sum(j => j.Belop),
                pJust.Where(j => j.Post == BudsjettPost.Timer).Sum(j => j.Belop),
                pJust.Where(j => j.Post == BudsjettPost.Uforutsett).Sum(j => j.Belop));

            resultat[pid] = o2 with { Status = Vurder(o2, inn, out var tekst), StatusTekst = tekst };
        }

        return resultat;
    }

    // Fordeler det kunden betaler på varer, arbeid og kjøring/tillegg.
    // Arbeid er resten, så summen alltid er lik den låste salgssummen også
    // om tilbudet er endret etter at budsjettet ble låst.
    private static void Fordel(TilbudBudsjett b, Tilbud t)
    {
        var varelinjer = t.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering).ToList();
        var varer = varelinjer.Sum(l => l.Utpris * l.Antall);
        var andre = 0m;
        if (t.BrukerKodelinjer && t.KodeLinjer.Count > 0)
        {
            andre = t.KodeLinjer
                .Where(l => !(l.Type == PrisKodeType.Arbeid && l.Enhet == PrisKodeEnhet.Time))
                .Sum(l => KodeLinjeBeregner.Sum(l, t.KodeLinjer, varer).Salg);
        }

        varer = Math.Clamp(Math.Round(varer, 2), 0, Math.Max(0, b.Salgssum));
        andre = Math.Clamp(Math.Round(andre, 2), 0, Math.Max(0, b.Salgssum - varer));
        b.SalgVarer = varer;
        b.SalgAndre = andre;
        b.SalgArbeid = b.Salgssum - varer - andre;
    }

    private static OkonomiStatus Vurder(Okonomi o, OkonomiInnstillinger inn, out string tekst)
    {
        if (o.Tilbud.Count == 0)
        {
            tekst = "Økonomien følges fra prosjektet er vunnet (godkjent tilbud).";
            return OkonomiStatus.IkkeVunnet;
        }

        static string P(decimal v) => $"{Math.Round(v * 100):0} %";

        if (o.JustertKost > 0 && o.FaktiskKost > o.JustertKost)
        {
            tekst = $"Kostnaden er allerede {P(o.FaktiskKost / o.JustertKost - 1)} over budsjettet.";
            return OkonomiStatus.OverBudsjett;
        }

        if (o.Fremdrift >= 0.999m && o.BudsjettTimer > 0 && o.FaktiskTimer <= 0)
        {
            tekst = "Ingen timer er registrert på prosjektet, så kosten er ufullstendig.";
            return OkonomiStatus.FolgMed;
        }

        var timerBrukt = o.BudsjettTimer > 0 ? o.FaktiskTimer / o.BudsjettTimer : 0;
        if (o.BudsjettTimer > 0 && timerBrukt >= inn.GulTimerBruktProsent / 100m && o.Fremdrift < inn.GulFremdriftProsent / 100m)
        {
            tekst = $"{P(timerBrukt)} av timene er brukt, men bare {P(o.Fremdrift)} er ferdig.";
            return OkonomiStatus.FolgMed;
        }

        if (o.JustertKost > 0 && o.PrognoseKost > o.JustertKost * (1 + inn.GulProsentOverBudsjett / 100m))
        {
            tekst = $"Prognosen ender {P(o.PrognoseKost / o.JustertKost - 1)} over budsjettet.";
            return OkonomiStatus.FolgMed;
        }

        tekst = o.Fremdrift >= 0.999m ? "Ferdig innenfor budsjettet." : "I rute mot budsjettet.";
        return OkonomiStatus.IRute;
    }

    // Antall aktive prosjekter som taper fortjeneste - til varselbjella.
    // Bare faktisk brukt mot tilbudt, som på Prosjektøkonomi.
    public async Task<int> AntallMedVarselAsync()
    {
        var ider = await AktiveVunneIderAsync();
        if (ider.Count == 0)
        {
            return 0;
        }

        return (await BeregnFlereAsync(ider)).Values.Count(o => o.TaperFortjeneste);
    }

    public async Task<int> AntallTilGjennomgangAsync() =>
        await db.Prosjekter.CountAsync(p => p.Status == ProsjektStatus.Overlevert && !db.ProsjektEtterkalkyler.Any(k => k.ProsjektId == p.Id && !k.ErUtkast));

    // Svarene på spørsmålene fra etterkalkylen i økonomiarket.
    public sealed record Evaluering(string? GikkBra, string? GikkDarlig, string? Forbedre, string? ForsinkelseEgne, string? ForsinkelseAndre);

    // Signerer etterkalkylen: tallene lagres slik de er nå.
    public Task<ProsjektEtterkalkyle> KontrollerAsync(int prosjektId, EtterkalkyleArsak arsak, string? kommentar, string? brukerNavn, Evaluering? evaluering = null) =>
        LagreGjennomgangAsync(prosjektId, arsak, kommentar, brukerNavn, evaluering, utkast: false);

    // Lagrer gjennomgangen uten å signere. Prosjektet blir stående under Til
    // gjennomgang. En signert gjennomgang blir ikke gjort om til utkast.
    public Task<ProsjektEtterkalkyle> LagreUtkastAsync(int prosjektId, EtterkalkyleArsak arsak, string? kommentar, string? brukerNavn, Evaluering? evaluering) =>
        LagreGjennomgangAsync(prosjektId, arsak, kommentar, brukerNavn, evaluering, utkast: true);

    private async Task<ProsjektEtterkalkyle> LagreGjennomgangAsync(int prosjektId, EtterkalkyleArsak arsak, string? kommentar, string? brukerNavn, Evaluering? evaluering, bool utkast)
    {
        static string? Rens(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        var o = await BeregnAsync(prosjektId);
        var k = await db.ProsjektEtterkalkyler.FirstOrDefaultAsync(x => x.ProsjektId == prosjektId) ?? new ProsjektEtterkalkyle { ProsjektId = prosjektId, ErUtkast = true };
        k.ErUtkast = utkast && k.ErUtkast;
        k.Kontrollert = DateTime.Now;
        k.KontrollertAv = brukerNavn;
        k.Arsak = arsak;
        k.Kommentar = Rens(kommentar);
        if (evaluering is not null)
        {
            k.GikkBra = Rens(evaluering.GikkBra);
            k.GikkDarlig = Rens(evaluering.GikkDarlig);
            k.Forbedre = Rens(evaluering.Forbedre);
            k.ForsinkelseEgne = Rens(evaluering.ForsinkelseEgne);
            k.ForsinkelseAndre = Rens(evaluering.ForsinkelseAndre);
        }

        k.Inntekt = o.Inntekt;
        k.BudsjettTimer = o.BudsjettTimer;
        k.FaktiskTimer = o.FaktiskTimer;
        k.BudsjettVarer = o.BudsjettVarer;
        k.FaktiskVarer = o.FaktiskVarer;
        k.BudsjettKost = o.BudsjettKost;
        k.FaktiskKost = o.FaktiskKost;
        k.BudsjettDekningsgrad = o.BudsjettDekningsgrad;
        k.FaktiskDekningsgrad = o.FaktiskDekningsgrad;
        if (k.Id == 0)
        {
            db.ProsjektEtterkalkyler.Add(k);
        }

        await db.SaveChangesAsync();
        return k;
    }

    // ---------- Lærdom ----------

    public sealed record Laerdom(int ComponentId, string Navn, int Prosjekter, decimal Forhold, int? NaMinutter, int? ForslagMinutter);

    // For hver vare i de kontrollerte prosjektene: hvor mye lengre (eller
    // kortere) tid prosjektene faktisk brukte enn beregnet, vektet etter hvor
    // mye av montasjetiden varen sto for. Et anslag - timene føres per
    // arbeidsordre, ikke per vare.
    public async Task<List<Laerdom>> LaerdomAsync(decimal terskel = 0.15m)
    {
        var kontrollerte = await db.ProsjektEtterkalkyler.AsNoTracking().Where(k => !k.ErUtkast && k.BudsjettTimer > 0 && k.FaktiskTimer > 0).ToListAsync();
        if (kontrollerte.Count == 0)
        {
            return [];
        }

        var forhold = kontrollerte.ToDictionary(k => k.ProsjektId, k => k.FaktiskTimer / k.BudsjettTimer);
        var pider = forhold.Keys.ToList();
        var linjer = await db.TilbudLinjer.AsNoTracking()
            .Where(l => pider.Contains(l.Tilbud!.ProsjektId) && l.Tilbud.Status == EndringsmeldingStatus.Godkjent
                && l.ComponentId != null && l.MontasjeMinutter > 0 && l.LevertAv == LevertAv.F && !l.ErGruppering)
            .Select(l => new { ProsjektId = l.Tilbud!.ProsjektId, ComponentId = l.ComponentId!.Value, Vekt = (decimal)(l.MontasjeMinutter ?? 0) * l.Antall })
            .ToListAsync();

        var perVare = linjer.GroupBy(l => l.ComponentId)
            .Select(g => new
            {
                ComponentId = g.Key,
                Prosjekter = g.Select(x => x.ProsjektId).Distinct().Count(),
                Forhold = g.Sum(x => x.Vekt * forhold[x.ProsjektId]) / g.Sum(x => x.Vekt)
            })
            .Where(x => x.Prosjekter >= 2 && Math.Abs(x.Forhold - 1) >= terskel)
            .ToList();
        var cider = perVare.Select(x => x.ComponentId).ToList();
        var varer = await db.Components.AsNoTracking().Where(c => cider.Contains(c.Id))
            .Select(c => new { c.Id, c.Navn, c.MontasjeMinutterProsjekt }).ToDictionaryAsync(c => c.Id);

        return perVare
            .Where(x => varer.ContainsKey(x.ComponentId))
            .Select(x =>
            {
                var c = varer[x.ComponentId];
                return new Laerdom(x.ComponentId, c.Navn, x.Prosjekter, Math.Round(x.Forhold, 2), c.MontasjeMinutterProsjekt,
                    c.MontasjeMinutterProsjekt is { } m ? (int)Math.Round(m * x.Forhold) : null);
            })
            .OrderByDescending(x => Math.Abs(x.Forhold - 1))
            .ToList();
    }
}
