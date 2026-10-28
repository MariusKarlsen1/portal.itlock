using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Pågående prosjekter under Prosjektøkonomi: det som var arket «Kalkulering
// og økonomi». Bygger på ProsjektOkonomiService (budsjett, faktisk og
// prognose) og legger til prosjektsum, fakturering med akonto,
// fremdriftsplanen og de fire spørsmålene øverst på prosjektet.
public class ProsjektOppfolgingService(ApplicationDbContext db, ProsjektOkonomiService okonomi)
{
    public enum Niva { Gronn, Gul, Rod, Noytral }

    // Et av de fire spørsmålene: «Tjener vi penger?» osv.
    public sealed record Svar(string Sporsmal, Niva Niva, string Overskrift, string Tekst);

    public sealed record AkontoRad(ProsjektAkonto Akonto, decimal Belop, bool Klar);

    public sealed record Endring(int Nr, Tilbud Tilbud, decimal Belop, int DagerVentet);

    public sealed record PlanOppgave(ProsjektplanOppgave Oppgave, ProsjektplanAktivitet Aktivitet, DateTime Frist, bool Forsinket);

    public sealed record Oppfolging(
        int ProsjektId,
        ProsjektOkonomiService.Okonomi? O,
        decimal Kontrakt,
        decimal Endringer,
        decimal Indeks,
        decimal Fakturert,
        decimal FakturertDenneMaaned,
        List<AkontoRad> Akontoer,
        List<Endring> Endringsmeldinger,
        FremdriftsplanBeregner.Frister? Frister,
        int Fristforlengelse,
        decimal? Tidsandel,
        bool HarPlan,
        decimal PlanAndel,
        int ForsinkedeAktiviteter,
        List<PlanOppgave> AapneOppgaver,
        List<Svar> Svar,
        string NesteSteg,
        Niva NesteStegNiva)
    {
        public decimal Prosjektsum => Kontrakt + Endringer + Indeks;
        public decimal Fremdrift => O?.Fremdrift ?? 0;
        // Det som er utført, regnet som andel av prosjektsummen.
        public decimal Opptjent => Math.Round(Prosjektsum * Fremdrift, 0);
        public decimal UtfortIkkeFakturert => Math.Max(0, Opptjent - Fakturert);
        public decimal Gjenstar => Math.Max(0, Prosjektsum - Fakturert);
        public decimal PlanlagtAkonto => Akontoer.Sum(a => a.Belop);
        public decimal Sluttoppgjor => Math.Max(0, Prosjektsum - PlanlagtAkonto);
        public AkontoRad? KlarAkonto => Akontoer.FirstOrDefault(a => a.Klar);
        public int EndringerVenter => Endringsmeldinger.Count(e => e.Tilbud.Status == EndringsmeldingStatus.SendtTilKunde && e.DagerVentet >= 7);
        public int Utfordringer => AapneOppgaver.Count(o => o.Oppgave.Status == PlanOppgaveStatus.Utfordring);

        // Fortjeneste igjen: prosjektsummen minus kosten per post, der en
        // post teller med det som er størst av rammen og det som er brukt.
        // Uten brukt over rammen er det fortjenesten slik den ble tilbudt
        // (pluss indeks, minus justeringer). Ingen prognose.
        public decimal TilbudtFortjeneste => O?.TilbudtFortjeneste ?? 0;
        public decimal FortjenesteIgjen => O is not { } o ? 0 : o.FortjenesteIgjen + Indeks;
        public decimal BruktOver => (O?.OverTimer ?? 0) + (O?.OverVarer ?? 0) + (O?.OverAndre ?? 0);

        // Grønn: innenfor. Gul: mer enn 5 % av fortjenesten er spist opp.
        // Rød: mer enn 25 % er spist opp, eller prosjektet går med tap.
        public Niva FortjenesteNiva
        {
            get
            {
                if (O is not { Inntekt: > 0 }) return Niva.Noytral;
                var tapt = TilbudtFortjeneste - FortjenesteIgjen;
                if (FortjenesteIgjen < 0 || (TilbudtFortjeneste > 0 && tapt > TilbudtFortjeneste * 0.25m)) return Niva.Rod;
                if (TilbudtFortjeneste <= 0 || tapt > TilbudtFortjeneste * 0.05m) return Niva.Gul;
                return Niva.Gronn;
            }
        }

        // Den posten som har spist mest av fortjenesten, i klartekst.
        public string? FortjenesteArsak
        {
            get
            {
                if (O is not { } o || BruktOver <= 0) return null;
                var (navn, over) = new[] { ("Timene", o.OverTimer), ("Varene", o.OverVarer), ("Kjøring og tillegg", o.OverAndre) }.MaxBy(x => x.Item2);
                var tillegg = navn == "Timene" && o.BudsjettTimer > 0
                    ? $" ({o.FaktiskTimer.ToString("0.#", ProsjektplanTekst.Kultur)} t brukt av {o.BudsjettTimer.ToString("0.#", ProsjektplanTekst.Kultur)} t)" : "";
                return $"{navn} er {over.ToString("#,##0", ProsjektplanTekst.Kultur)} kr over tilbudet{tillegg}";
            }
        }
    }

    public async Task<Oppfolging> HentAsync(int prosjektId) => (await HentFlereAsync([prosjektId]))[prosjektId];

    public async Task<Dictionary<int, Oppfolging>> HentFlereAsync(IReadOnlyCollection<int> prosjektIder)
    {
        var ider = prosjektIder.Distinct().ToList();
        var idag = DateTime.Today;
        var o = ider.Count == 0 ? [] : await okonomi.BeregnFlereAsync(ider);

        var prosjekter = await db.Prosjekter.AsNoTracking().Where(p => ider.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var akontoer = await db.ProsjektAkontoer.AsNoTracking().Where(a => ider.Contains(a.ProsjektId)).OrderBy(a => a.Nr).ToListAsync();
        var endringer = await db.Tilbud.AsNoTracking().Include(t => t.Linjer)
            .Where(t => ider.Contains(t.ProsjektId) && t.Type == TilbudType.Endringsmelding)
            .OrderBy(t => t.Id).ToListAsync();
        var aktiviteter = await db.ProsjektplanAktiviteter.AsNoTracking().Include(a => a.Oppgaver)
            .Where(a => ider.Contains(a.ProsjektId)).ToListAsync();

        // Fakturert: akontoer som er sendt, og arbeidsordre som er fakturert eller sendt til Tripletex.
        var koder = await db.PrisKoder.AsNoTracking().ToListAsync();
        var fakturerteAo = await db.Arbeidsordre.AsNoTracking().Include(a => a.Tilbud!).ThenInclude(t => t.Linjer).Include(a => a.Varer)
            .Include(a => a.Tilbud!).ThenInclude(t => t.KodeLinjer).Include(a => a.KodeLinjer).Include(a => a.Timeregistreringer)
            .Where(a => a.ProsjektId != null && ider.Contains(a.ProsjektId.Value) && (a.FakturertDato != null || a.TripletexOrdreId != null))
            .ToListAsync();

        var resultat = new Dictionary<int, Oppfolging>();
        foreach (var pid in ider)
        {
            if (!prosjekter.TryGetValue(pid, out var p))
            {
                continue;
            }

            var ok = o.GetValueOrDefault(pid) is { Tilbud.Count: > 0 } x ? x : null;
            var kontrakt = ok?.Tilbud.Where(t => !t.ErEndring).Sum(t => t.Salgssum) ?? 0;
            var tillegg = ok?.Tilbud.Where(t => t.ErEndring).Sum(t => t.Salgssum) ?? 0;
            var indeks = p.Indeksregulering ?? 0;
            var fremdrift = ok?.Fremdrift ?? 0;

            var pAo = fakturerteAo.Where(a => a.ProsjektId == pid).Select(a => new
            {
                Dato = a.FakturertDato ?? a.TripletexOrdreSendtDato,
                Belop = ArbeidsordreOkonomiBeregner.BeregnLinjer(a, koder).Sum(l => l.Belop)
            }).ToList();

            // Akontoer: beløp fra prosent av kontrakten. Klar når milepælen er
            // nådd, men bare så lenge det er noe igjen å fakturere, så det som
            // allerede er fakturert via arbeidsordrene ikke faktureres to ganger.
            var pAkonto = new List<AkontoRad>();
            var igjen = kontrakt + tillegg + indeks - pAo.Sum(a => a.Belop);
            foreach (var a in akontoer.Where(a => a.ProsjektId == pid))
            {
                var belop = a.Status != AkontoStatus.Planlagt && a.SendtBelop is { } sendt ? sendt
                    : a.Prosent is { } pst ? Math.Round(kontrakt * pst / 100m, 0) : a.Belop ?? 0;
                if (a.Status != AkontoStatus.Planlagt)
                {
                    igjen -= belop;
                }

                pAkonto.Add(new AkontoRad(a, belop, false));
            }

            for (var i = 0; i < pAkonto.Count; i++)
            {
                var (a, belop) = (pAkonto[i].Akonto, pAkonto[i].Belop);
                var naadd = (a.FremdriftProsent is { } fp && fremdrift * 100 >= fp) || (a.PlanlagtDato is { } d && d.Date <= idag);
                if (a.Status == AkontoStatus.Planlagt && belop > 0 && naadd && belop <= igjen + 1)
                {
                    pAkonto[i] = pAkonto[i] with { Klar = true };
                    igjen -= belop;
                }
            }

            var fakturert = pAkonto.Where(a => a.Akonto.Status != AkontoStatus.Planlagt).Sum(a => a.Belop) + pAo.Sum(a => a.Belop);
            var maanedStart = new DateTime(idag.Year, idag.Month, 1);
            var denneMaaned = pAkonto.Where(a => a.Akonto.SendtDato >= maanedStart).Sum(a => a.Belop) + pAo.Where(a => a.Dato >= maanedStart).Sum(a => a.Belop);

            var nr = 0;
            var pEndringer = endringer.Where(t => t.ProsjektId == pid).Select(t =>
            {
                var belop = ok?.Tilbud.FirstOrDefault(b => b.Id == t.Id)?.Salgssum ?? t.SendtSum ?? TilbudBeregningHelper.BeregnTotaltUtenMva(t);
                var ventet = t.Status == EndringsmeldingStatus.SendtTilKunde && t.SendtDato is { } s ? (int)(idag - s.Date).TotalDays : 0;
                return new Endring(++nr, t, Math.Round(belop, 0), ventet);
            }).ToList();
            var forlengelse = pEndringer.Where(e => e.Tilbud.Status == EndringsmeldingStatus.Godkjent).Sum(e => e.Tilbud.FristforlengelseDager ?? 0);

            // Fremdriftsplanen.
            var frister = FremdriftsplanBeregner.FristerFor(p, forlengelse);
            var pAkt = aktiviteter.Where(a => a.ProsjektId == pid).OrderBy(a => a.Fase).ThenBy(a => a.Rekkefolge).ToList();
            var forsinkede = 0;
            var aapne = new List<PlanOppgave>();
            if (frister is not null && pAkt.Count > 0)
            {
                var faser = FremdriftsplanBeregner.Faser(frister);
                var mandag = FremdriftsplanBeregner.Mandag(idag);
                var sondag = mandag.AddDays(6);
                foreach (var a in pAkt)
                {
                    var (fra, til) = FremdriftsplanBeregner.AktivitetDatoer(FremdriftsplanBeregner.FaseFor(faser, a.Fase), a.DelFra, a.DelTil);
                    var andel = FremdriftsplanBeregner.Andel(a.Oppgaver.Select(t => t.Status));
                    var sen = til < idag && andel < 1;
                    if (sen) forsinkede++;
                    if (sen || (fra <= sondag && til >= mandag))
                    {
                        aapne.AddRange(a.Oppgaver.Where(t => !t.Status.ErFerdig()).OrderBy(t => t.Rekkefolge).Select(t => new PlanOppgave(t, a, til, sen)));
                    }
                }
            }

            var planAndel = pAkt.Count == 0 ? 0 : pAkt.GroupBy(a => a.Fase)
                .Average(g => g.Average(a => FremdriftsplanBeregner.Andel(a.Oppgaver.Select(t => t.Status))));

            decimal? tidsandel = null;
            if (frister is not null && frister.Overlevering > frister.Montasjestart)
            {
                tidsandel = Math.Clamp((decimal)((idag - frister.Montasjestart).TotalDays / (frister.Overlevering - frister.Montasjestart).TotalDays), 0, 1);
            }

            var f = new Oppfolging(pid, ok, kontrakt, tillegg, indeks, Math.Round(fakturert, 0), Math.Round(denneMaaned, 0), pAkonto, pEndringer,
                frister, forlengelse, tidsandel, pAkt.Count > 0, planAndel, forsinkede, aapne, [], "", Niva.Noytral);
            var svar = LagSvar(f, p, idag);
            var (neste, niva) = NesteSteg(f);
            resultat[pid] = f with { Svar = svar, NesteSteg = neste, NesteStegNiva = niva };
        }

        return resultat;
    }

    private static string Kr(decimal v) => v.ToString("#,##0", ProsjektplanTekst.Kultur) + " kr";

    private static string Pst(decimal v) => Math.Round(v * 100).ToString("0", ProsjektplanTekst.Kultur) + " %";

    private static string DG(decimal? v) => ((v ?? 0) * 100).ToString("0.0", ProsjektplanTekst.Kultur) + " %";

    private static List<Svar> LagSvar(Oppfolging f, Prosjekt p, DateTime idag)
    {
        var liste = new List<Svar>();
        var o = f.O;

        // 1. Tjener vi penger?
        if (o is not { Inntekt: > 0 })
        {
            liste.Add(new("Tjener vi penger?", Niva.Noytral, "Ikke ennå", "Budsjettet låses når et tilbud på prosjektet settes til Godkjent."));
        }
        else
        {
            // Bare faktisk brukt mot tilbudt: fortjenesten som er igjen.
            var niva = f.FortjenesteNiva;
            var overskrift = f.FortjenesteIgjen < 0 ? $"Nei, {Kr(f.FortjenesteIgjen)}" : $"{Kr(f.FortjenesteIgjen)} igjen";
            var tekst = $"Av {Kr(f.TilbudtFortjeneste)} i tilbudet ({DG(o.BudsjettDekningsgrad)}). " + (f.FortjenesteArsak is { } a ? a + "." : "Ingenting er brukt mer enn tilbudt.");
            liste.Add(new("Tjener vi penger?", niva, overskrift, tekst));
        }

        // 2. Er vi i rute?
        if (f.Frister is not { } fr || f.Tidsandel is not { } tid)
        {
            liste.Add(new("Er vi i rute?", Niva.Noytral, "Mangler frister", "Legg inn montasjestart og overleveringsfrist under Fremdriftsplan, så sammenlignes utført med tiden som er brukt."));
        }
        else
        {
            var ukerIgjen = FremdriftsplanBeregner.UkerMellom(idag, fr.Overlevering);
            var fristTekst = ukerIgjen >= 0 ? $"{ukerIgjen} uker til overlevering {fr.Overlevering:dd.MM}." : $"Overleveringsfristen {fr.Overlevering:dd.MM} er passert.";
            if (idag > fr.Overlevering && f.Fremdrift < 0.999m)
            {
                liste.Add(new("Er vi i rute?", Niva.Rod, "Nei, fristen er passert", $"{Pst(f.Fremdrift)} er utført. {fristTekst}"));
            }
            else if (f.Fremdrift + 0.1m < tid || f.ForsinkedeAktiviteter > 0)
            {
                var sen = f.ForsinkedeAktiviteter > 0 ? $" {f.ForsinkedeAktiviteter} aktivitet{(f.ForsinkedeAktiviteter == 1 ? " er" : "er er")} forsinket i planen." : "";
                liste.Add(new("Er vi i rute?", Niva.Gul, "Følg med", $"{Pst(f.Fremdrift)} er utført etter {Pst(tid)} av montasjetiden.{sen} {fristTekst}"));
            }
            else
            {
                liste.Add(new("Er vi i rute?", Niva.Gronn, f.Fremdrift > tid + 0.05m ? "Ja, foran planen" : "Ja", $"{Pst(f.Fremdrift)} er utført etter {Pst(tid)} av montasjetiden. {fristTekst}"));
            }
        }

        // 3. Har vi fakturert nok?
        if (f.Prosjektsum <= 0)
        {
            liste.Add(new("Har vi fakturert nok?", Niva.Noytral, "Ingen prosjektsum", "Prosjektsummen kommer fra godkjente tilbud og endringsmeldinger."));
        }
        else if (f.KlarAkonto is { } klar)
        {
            liste.Add(new("Har vi fakturert nok?", Niva.Gul, $"Akonto {klar.Akonto.Nr} er klar", $"{klar.Akonto.Milepael}: {Kr(klar.Belop)} kan sendes til Tripletex. Utført, ikke fakturert: {Kr(f.UtfortIkkeFakturert)}."));
        }
        else if (f.UtfortIkkeFakturert > f.Prosjektsum * 0.05m)
        {
            liste.Add(new("Har vi fakturert nok?", Niva.Gul, $"{Kr(f.UtfortIkkeFakturert)} mangler", $"Utført for {Kr(f.Opptjent)}, fakturert {Kr(f.Fakturert)}.{(f.Akontoer.Count == 0 ? " Lag en faktureringsplan under Fakturering." : "")}"));
        }
        else
        {
            liste.Add(new("Har vi fakturert nok?", Niva.Gronn, "Ja", $"Fakturert {Kr(f.Fakturert)} av {Kr(f.Prosjektsum)} ({Pst(f.Prosjektsum > 0 ? f.Fakturert / f.Prosjektsum : 0)})."));
        }

        // 4. Venter noe på oss?
        var ting = new List<string>();
        if (f.ForsinkedeAktiviteter > 0) ting.Add($"{f.AapneOppgaver.Count(a => a.Forsinket)} oppgaver er forsinket");
        if (f.Utfordringer > 0) ting.Add($"{f.Utfordringer} oppgave{(f.Utfordringer == 1 ? "" : "r")} har en utfordring");
        var denneUken = f.AapneOppgaver.Count(a => !a.Forsinket);
        if (denneUken > 0) ting.Add($"{denneUken} oppgaver skal gjøres denne uken");
        foreach (var e in f.Endringsmeldinger.Where(e => e.Tilbud.Status == EndringsmeldingStatus.SendtTilKunde && e.DagerVentet >= 7))
        {
            ting.Add($"E{e.Nr} har ventet på kunden i {e.DagerVentet} dager");
        }
        if (o is { AntallIkkeFakturert: > 0 }) ting.Add($"{o.AntallIkkeFakturert} ferdigmeldte arbeidsordre er ikke fakturert");
        if (p.BankgarantiTil is { } bg && bg.Date <= idag.AddDays(14)) ting.Add($"bankgarantien går ut {bg:dd.MM}");

        liste.Add(ting.Count == 0
            ? new("Venter noe på oss?", Niva.Gronn, "Nei", f.HarPlan ? "Ingen åpne oppgaver denne uken." : "Lag fremdriftsplanen for å få oppgavene her.")
            : new("Venter noe på oss?", f.ForsinkedeAktiviteter > 0 || f.Utfordringer > 0 ? Niva.Rod : Niva.Gul, "Ja", Stor(string.Join(", ", ting)) + "."));
        return liste;
    }

    private static string Stor(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];

    private static (string, Niva) NesteSteg(Oppfolging f)
    {
        if (f.O is null)
        {
            return ("Mangler godkjent tilbud", Niva.Noytral);
        }

        if (f.FortjenesteNiva == Niva.Rod)
        {
            return (f.FortjenesteIgjen < 0 ? "Taper penger · lag endringsmelding?" : "Taper fortjeneste · se hvorfor", Niva.Rod);
        }

        if (f.ForsinkedeAktiviteter > 0)
        {
            return ($"{f.ForsinkedeAktiviteter} forsinket i planen", Niva.Rod);
        }

        if (f.FortjenesteNiva == Niva.Gul)
        {
            return ("Følg med på kostnadene", Niva.Gul);
        }

        if (f.KlarAkonto is { } klar)
        {
            return ($"Send akonto {klar.Akonto.Nr} · {Kr(klar.Belop)}", Niva.Gul);
        }

        if (f.EndringerVenter > 0)
        {
            return ($"{f.EndringerVenter} endring{(f.EndringerVenter == 1 ? "" : "er")} venter på kunden", Niva.Gul);
        }

        if (f.O.AntallIkkeFakturert > 0)
        {
            return ($"{Kr(f.O.IkkeFakturert)} ikke fakturert", Niva.Gul);
        }

        if (f.Frister is null)
        {
            return ("Legg inn frister", Niva.Noytral);
        }

        if (f.Akontoer.Count == 0 && f.Prosjektsum > 0)
        {
            return ("Lag faktureringsplan", Niva.Noytral);
        }

        return ("I rute", Niva.Gronn);
    }

    // ---------- Endringer fra skjermen ----------

    // Lager fremdriftsplanen fra malen. Setter frister som mangler, så planen
    // kan tegnes med en gang.
    public async Task LagPlanFraMalAsync(int prosjektId)
    {
        if (await db.ProsjektplanAktiviteter.AnyAsync(a => a.ProsjektId == prosjektId))
        {
            return;
        }

        var p = await db.Prosjekter.FirstAsync(x => x.Id == prosjektId);
        var idag = DateTime.Today;
        p.StartDato ??= p.OpprettetDato.ToLocalTime().Date;
        p.MontasjestartDato ??= p.StartDato.Value.AddDays(21) > idag ? p.StartDato.Value.AddDays(21) : FremdriftsplanBeregner.Mandag(idag);
        p.OverleveringsfristDato ??= p.MontasjestartDato.Value.AddDays(56);

        // Lages planen på et prosjekt som er i gang, regnes det som allerede
        // er passert som avsluttet. Det kan endres per oppgave etterpå.
        var faser = FremdriftsplanBeregner.Faser(FremdriftsplanBeregner.FristerFor(p)!);
        var rekkefolge = 0;
        foreach (var m in FremdriftsplanBeregner.Mal)
        {
            var passert = FremdriftsplanBeregner.AktivitetDatoer(FremdriftsplanBeregner.FaseFor(faser, m.Fase), m.DelFra, m.DelTil).Til < idag;
            var a = new ProsjektplanAktivitet
            {
                ProsjektId = prosjektId,
                Fase = m.Fase,
                Navn = m.Navn,
                Rolle = m.Rolle,
                DelFra = m.DelFra,
                DelTil = m.DelTil,
                Rekkefolge = rekkefolge++
            };
            var nr = 0;
            foreach (var t in m.Oppgaver)
            {
                a.Oppgaver.Add(new ProsjektplanOppgave
                {
                    Tekst = t.Tekst,
                    Rolle = t.Rolle,
                    Rekkefolge = nr++,
                    Status = passert ? PlanOppgaveStatus.Avsluttet : PlanOppgaveStatus.IkkeStartet
                });
            }

            db.ProsjektplanAktiviteter.Add(a);
        }

        // Prosjektlederen på prosjektet får rollen med en gang.
        if (p.ProsjektlederId is { } pl && !await db.ProsjektRoller.AnyAsync(r => r.ProsjektId == prosjektId && r.Rolle == "Prosjektleder"))
        {
            db.ProsjektRoller.Add(new ProsjektRolle { ProsjektId = prosjektId, Rolle = "Prosjektleder", BrukerId = pl });
        }

        await db.SaveChangesAsync();
    }

    // Standard faktureringsplan som i arket: 20/30/25/15 % og resten i sluttoppgjøret.
    public async Task ForeslaAkontoplanAsync(int prosjektId)
    {
        if (await db.ProsjektAkontoer.AnyAsync(a => a.ProsjektId == prosjektId))
        {
            return;
        }

        db.ProsjektAkontoer.AddRange(
            new ProsjektAkonto { ProsjektId = prosjektId, Nr = 1, Milepael = "Kontrakt signert", Prosent = 20, FremdriftProsent = 0 },
            new ProsjektAkonto { ProsjektId = prosjektId, Nr = 2, Milepael = "Varer levert", Prosent = 30, FremdriftProsent = 25 },
            new ProsjektAkonto { ProsjektId = prosjektId, Nr = 3, Milepael = "Montasje 50 % ferdig", Prosent = 25, FremdriftProsent = 50 },
            new ProsjektAkonto { ProsjektId = prosjektId, Nr = 4, Milepael = "Overlevering", Prosent = 15, FremdriftProsent = 100 });
        await db.SaveChangesAsync();
    }

    // ======================================================================
    // Overlevering og etterkalkyle

    public sealed record SjekkPunkt(bool Ok, string Tittel, string Tekst, string? Handling = null);

    // Det som bør være på plass før prosjektet settes som overlevert. Ingen av
    // punktene stopper overleveringen; de vises som en påminnelse.
    public async Task<List<SjekkPunkt>> OverleveringSjekkAsync(int prosjektId)
    {
        var f = await HentAsync(prosjektId);
        var p = await db.Prosjekter.AsNoTracking().FirstAsync(x => x.Id == prosjektId);
        var dorer = await db.Dorer.AsNoTracking().Where(d => d.ProsjektId == prosjektId)
            .GroupBy(d => 1).Select(g => new { Alle = g.Count(), Ferdig = g.Count(d => d.Status == MontasjeStatus.FerdigMontert) }).FirstOrDefaultAsync();
        var liste = new List<SjekkPunkt>();

        if (dorer is { Alle: > 0 })
        {
            liste.Add(new(dorer.Ferdig == dorer.Alle, dorer.Ferdig == dorer.Alle ? "Alle dører er ferdig montert" : "Ikke alle dører er ferdig montert",
                $"{dorer.Ferdig} av {dorer.Alle} dører"));
        }

        if (f.O is null)
        {
            liste.Add(new(false, "Prosjektet har ikke noe godkjent tilbud",
                "Etterkalkylen får ingen kalkyle å sammenligne med. Finnes det et tilbud, sett det til Godkjent først.", "tilbud"));
        }

        if (f.O is { } o)
        {
            liste.Add(o.FaktiskTimerVenter > 0
                ? new(false, "Noen timer venter på godkjenning", $"{o.FaktiskTimerVenter.ToString("0.#", ProsjektplanTekst.Kultur)} t venter. De er tatt med i tallene.", "timer")
                : new(true, "Alle timer er godkjent", $"{o.FaktiskTimer.ToString("0.#", ProsjektplanTekst.Kultur)} t på prosjektet"));
            liste.Add(o.VarerFraLager || o.VarerManuell
                ? new(true, "Varekost er registrert", o.VarerFraLager ? "Faktisk forbruk fra lageret" : $"Lagt inn: {Kr(o.FaktiskVarer)}")
                : new(false, "Varer er anslått, ikke registrert", "Legg inn faktisk varekost, for eksempel fra Tripletex, så blir etterkalkylen riktig.", "varer"));
        }

        if (f.Prosjektsum > 0)
        {
            liste.Add(f.Gjenstar <= 1
                ? new(true, "Prosjektsummen er fakturert", $"{Kr(f.Fakturert)} av {Kr(f.Prosjektsum)}")
                : new(false, "Ikke alt er fakturert", $"{Kr(f.Gjenstar)} gjenstår av prosjektsummen {Kr(f.Prosjektsum)}", "faktura"));
        }

        if (p.BankgarantiBelop is > 0)
        {
            liste.Add(new(true, "Husk å nedskrive bankgarantien", "Vanlig er 3 % av kontraktsummen i reklamasjonstiden."));
        }

        return liste;
    }

    // Prosjektet flyttes til Etterkalkyle. E-posten til prosjektleder og
    // administratorene sendes fra ApplicationDbContext når statusen lagres.
    public async Task SettOverlevertAsync(int prosjektId, DateTime dato)
    {
        var p = await db.Prosjekter.FirstAsync(x => x.Id == prosjektId);
        p.Status = ProsjektStatus.Overlevert;
        p.OverlevertDato = dato.Date;
        await db.SaveChangesAsync();
    }

    public async Task SettFaktiskVarekostAsync(int prosjektId, decimal? belop)
    {
        var p = await db.Prosjekter.FirstAsync(x => x.Id == prosjektId);
        p.FaktiskVarekost = belop is >= 0 ? belop : null;
        await db.SaveChangesAsync();
    }

    public enum Dom { Bedre, Som, Darligere }

    public sealed record AvvikBidrag(string Tekst, decimal Belop, bool Total = false);

    public sealed record Etterkalkyle(
        int ProsjektId,
        ProsjektOkonomiService.Okonomi O,
        decimal Kontrakt,
        decimal Endringer,
        decimal Indeks,
        decimal EndringKost,
        decimal Fakturert,
        DateTime? Frist,
        DateTime? Overlevert,
        int PlanFerdig,
        int PlanTotal,
        List<ProsjektOppfolgingService.Endring> Endringsmeldinger)
    {
        // Kalkyle = godkjente tilbud og endringer, slik budsjettet ble låst.
        public decimal KalkInntekt => Kontrakt + Endringer;
        public decimal KalkVarer => O.BudsjettVarer;
        public decimal KalkLonn => O.BudsjettTimekost;
        public decimal KalkAndre => O.BudsjettAndre;
        public decimal KalkKost => KalkVarer + KalkLonn + KalkAndre;
        public decimal KalkResultat => KalkInntekt - KalkKost;
        public decimal? KalkDG => KalkInntekt > 0 ? KalkResultat / KalkInntekt : null;
        // Det som ble regnet med da kontrakten ble signert, uten endringene.
        public decimal KontraktResultat => Kontrakt - (KalkKost - EndringKost);
        public decimal EndringResultat => Endringer - EndringKost;

        public decimal FaktiskInntekt => Kontrakt + Endringer + Indeks;
        public decimal FaktiskVarer => O.FaktiskVarer;
        public decimal FaktiskLonn => O.FaktiskTimekost;
        public decimal FaktiskAndre => O.FaktiskAndre;
        public decimal FaktiskKost => FaktiskVarer + FaktiskLonn + FaktiskAndre;
        public decimal FaktiskResultat => FaktiskInntekt - FaktiskKost;
        public decimal? FaktiskDG => FaktiskInntekt > 0 ? FaktiskResultat / FaktiskInntekt : null;

        public decimal ResultatAvvik => FaktiskResultat - KalkResultat;
        public decimal DgPoeng => ((FaktiskDG ?? 0) - (KalkDG ?? 0)) * 100;
        public decimal TimerAvvik => O.FaktiskTimer - O.BudsjettTimer;
        public decimal VarerAvvik => FaktiskVarer - KalkVarer;
        public decimal LonnAvvik => FaktiskLonn - KalkLonn;
        public decimal AndreAvvik => FaktiskAndre - KalkAndre;
        public bool VarerAnslatt => !O.VarerFraLager && !O.VarerManuell;
        public int? DagerOverFrist => Frist is { } f && Overlevert is { } o ? (int)(o.Date - f.Date).TotalDays : null;

        public Dom Dom => FaktiskResultat < 0 || DgPoeng <= -3 ? Dom.Darligere : DgPoeng >= 2 ? Dom.Bedre : Dom.Som;

        public string DomTekst => Dom switch
        {
            Dom.Bedre => "Bedre enn kalkulert",
            Dom.Darligere => "Dårligere enn kalkulert",
            _ => "Som kalkulert"
        };

        // Fra kalkulert resultat ved kontrakt til faktisk resultat.
        public List<AvvikBidrag> Bidrag
        {
            get
            {
                var liste = new List<AvvikBidrag> { new("Kalkulert ved kontrakt", KontraktResultat, true) };
                if (Endringer != 0 || EndringKost != 0) liste.Add(new("Endringer", EndringResultat));
                if (Indeks != 0) liste.Add(new("Indeksregulering", Indeks));
                liste.Add(new(Math.Abs(VarerAvvik) < 1 ? "Materiell som kalkulert" : VarerAvvik > 0 ? "Materiell over kalkyle" : "Materiell under kalkyle", -VarerAvvik));
                liste.Add(new(Math.Abs(LonnAvvik) < 1 ? "Timer som kalkulert" : LonnAvvik > 0 ? $"Timer over kalkyle ({TimerTekst(TimerAvvik)})" : $"Timer under kalkyle ({TimerTekst(TimerAvvik)})", -LonnAvvik));
                if (Math.Abs(AndreAvvik) >= 1) liste.Add(new(AndreAvvik > 0 ? "Kjøring og tillegg over kalkyle" : "Kjøring og tillegg under kalkyle", -AndreAvvik));
                liste.Add(new("Faktisk resultat", FaktiskResultat, true));
                return liste;
            }
        }

        // Det største avviket på kostnadene, til lista og forklaringen.
        public (string Tekst, Niva Niva) StorsteAvvik
        {
            get
            {
                var timer = (Belop: -LonnAvvik, Tekst: $"Timer {(TimerAvvik >= 0 ? "+" : "−")}{Math.Abs(TimerAvvik).ToString("0", ProsjektplanTekst.Kultur)} t");
                var varer = (Belop: -VarerAvvik, Tekst: $"Varer {(VarerAvvik >= 0 ? "+" : "−")}{Math.Abs(VarerAvvik).ToString("#,##0", ProsjektplanTekst.Kultur)}");
                var storst = Math.Abs(timer.Belop) >= Math.Abs(varer.Belop) ? timer : varer;
                if (Math.Abs(storst.Belop) < Math.Max(1000, KalkInntekt * 0.005m))
                {
                    return ("Ingen vesentlige", Niva.Gronn);
                }

                return (storst.Tekst, storst.Belop > 0 ? Niva.Gronn : -storst.Belop > KalkInntekt * 0.03m ? Niva.Rod : Niva.Gul);
            }
        }

        public string Forklaring
        {
            get
            {
                var deler = new List<string>
                {
                    Math.Abs(ResultatAvvik) < 1000
                        ? "Resultatet ble som kalkulert."
                        : $"Resultatet ble {Kr(Math.Abs(ResultatAvvik))} {(ResultatAvvik > 0 ? "bedre" : "dårligere")} enn kalkylen."
                };
                if (Math.Abs(TimerAvvik) >= 1)
                {
                    deler.Add($"Jobben brukte {TimerTekst(Math.Abs(TimerAvvik))} {(TimerAvvik > 0 ? "mer" : "mindre")} enn beregnet ({(LonnAvvik > 0 ? "−" : "+")}{Kr(Math.Abs(LonnAvvik))}).");
                }

                if (Math.Abs(VarerAvvik) >= 1000)
                {
                    deler.Add($"Materiell kostet {Kr(Math.Abs(VarerAvvik))} {(VarerAvvik > 0 ? "mer" : "mindre")} enn kalkulert.");
                }

                if (Endringer != 0)
                {
                    deler.Add($"Endringene ga {Kr(Endringer)} i tillegg.");
                }

                if (VarerAnslatt)
                {
                    deler.Add("Varene er anslått, så avviket på varer er ikke kjent.");
                }

                return string.Join(" ", deler);
            }
        }

        // Forslag til årsak ut fra det største avviket.
        public EtterkalkyleArsak ForeslattArsak
        {
            get
            {
                if (Dom == Dom.Som && (DagerOverFrist ?? 0) <= 0)
                {
                    return EtterkalkyleArsak.IngenAvvik;
                }

                if (LonnAvvik <= 0 && VarerAvvik <= 0)
                {
                    return (DagerOverFrist ?? 0) > 0 ? EtterkalkyleArsak.Ventetid : EtterkalkyleArsak.IngenAvvik;
                }

                return VarerAvvik > LonnAvvik ? EtterkalkyleArsak.Varekostnad
                    : Endringer > 0 ? EtterkalkyleArsak.Ekstraarbeid : EtterkalkyleArsak.FeilITilbud;
            }
        }

        // Forsinkelser som skyldes andre, fra fristforlengelsene på endringene.
        public string? ForslagForsinkelseAndre
        {
            get
            {
                var med = Endringsmeldinger.Where(e => e.Tilbud.Status == EndringsmeldingStatus.Godkjent && e.Tilbud.FristforlengelseDager is > 0).ToList();
                return med.Count == 0 ? null : string.Join("; ", med.Select(e =>
                    $"E{e.Nr}: {e.Tilbud.FristforlengelseDager} dager{(string.IsNullOrWhiteSpace(e.Tilbud.BerorteFag) ? "" : $" ({e.Tilbud.BerorteFag})")}"));
            }
        }

        private static string TimerTekst(decimal t) => $"{Math.Abs(t).ToString("0", ProsjektplanTekst.Kultur)} t";
    }

    public async Task<Etterkalkyle?> EtterkalkyleAsync(int prosjektId) =>
        (await EtterkalkyleFlereAsync([prosjektId])).GetValueOrDefault(prosjektId);

    public async Task<Dictionary<int, Etterkalkyle>> EtterkalkyleFlereAsync(IReadOnlyCollection<int> prosjektIder)
    {
        var oppfolging = await HentFlereAsync(prosjektIder);
        var ider = oppfolging.Keys.ToList();
        var overlevert = await db.Prosjekter.AsNoTracking().Where(p => ider.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.OverlevertDato);
        var plan = (await db.ProsjektplanOppgaver.AsNoTracking()
                .Where(t => ider.Contains(t.Aktivitet!.ProsjektId))
                .Select(t => new { t.Aktivitet!.ProsjektId, t.Status }).ToListAsync())
            .GroupBy(t => t.ProsjektId)
            .ToDictionary(g => g.Key, g => (Ferdig: g.Count(t => t.Status.ErFerdig()), Alle: g.Count()));

        var resultat = new Dictionary<int, Etterkalkyle>();
        foreach (var (id, f) in oppfolging)
        {
            if (f.O is not { Inntekt: > 0 } o)
            {
                continue;
            }

            // Kostnaden som var regnet inn i endringene, med samme timekost som resten av budsjettet.
            var sats = o.BudsjettTimer > 0 ? o.BudsjettTimekost / o.BudsjettTimer : o.Timesats;
            var endringKost = o.Tilbud.Where(t => t.ErEndring).Sum(t => t.Varekost + t.Timer * sats + t.AndreKost);
            var (ferdig, alle) = plan.GetValueOrDefault(id);
            resultat[id] = new Etterkalkyle(id, o, f.Kontrakt, f.Endringer, f.Indeks, Math.Round(endringKost, 0), f.Fakturert,
                f.Frister?.Overlevering, overlevert.GetValueOrDefault(id), ferdig, alle, f.Endringsmeldinger);
        }

        return resultat;
    }
}

public static class ProsjektplanTekst
{
    public static readonly System.Globalization.CultureInfo Kultur = System.Globalization.CultureInfo.GetCultureInfo("nb-NO");
}
