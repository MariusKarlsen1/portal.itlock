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
        var fakturerteAo = await db.Arbeidsordre.AsNoTracking().Include(a => a.Tilbud!).ThenInclude(t => t.Linjer).Include(a => a.Varer)
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
                Belop = ArbeidsordreOkonomiBeregner.BeregnLinjer(a).Sum(l => l.Belop)
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
                    var (fra, til) = FremdriftsplanBeregner.AktivitetDatoer(faser[Math.Clamp(a.Fase, 0, 3)], a.DelFra, a.DelTil);
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
        else if (o.Status == ProsjektOkonomiService.OkonomiStatus.OverBudsjett)
        {
            liste.Add(new("Tjener vi penger?", Niva.Rod, $"Nei, {DG(o.PrognoseDekningsgrad)} DG", o.StatusTekst));
        }
        else if ((o.PrognoseDekningsgrad ?? 0) < (o.BudsjettDekningsgrad ?? 0) - 0.1m)
        {
            liste.Add(new("Tjener vi penger?", Niva.Rod, $"For lite, {DG(o.PrognoseDekningsgrad)} DG", $"Tilbudt {DG(o.BudsjettDekningsgrad)}. {o.StatusTekst}"));
        }
        else if ((o.PrognoseDekningsgrad ?? 0) < (o.BudsjettDekningsgrad ?? 0) - 0.02m || o.Status == ProsjektOkonomiService.OkonomiStatus.FolgMed)
        {
            liste.Add(new("Tjener vi penger?", Niva.Gul, $"Ja, {DG(o.PrognoseDekningsgrad)} DG", $"Tilbudt {DG(o.BudsjettDekningsgrad)}. {o.StatusTekst}"));
        }
        else
        {
            liste.Add(new("Tjener vi penger?", Niva.Gronn, $"Ja, {DG(o.PrognoseDekningsgrad)} DG", $"Tilbudt {DG(o.BudsjettDekningsgrad)}. Prognosen er {Kr(o.PrognoseKost)} mot budsjett {Kr(o.JustertKost)}."));
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

        if (f.O.Status == ProsjektOkonomiService.OkonomiStatus.OverBudsjett)
        {
            return ("Over budsjett · se prognosen", Niva.Rod);
        }

        if (f.ForsinkedeAktiviteter > 0)
        {
            return ($"{f.ForsinkedeAktiviteter} forsinket i planen", Niva.Rod);
        }

        if (f.O.Status == ProsjektOkonomiService.OkonomiStatus.FolgMed)
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
        p.TilbudsfristDato ??= p.StartDato.Value.AddDays(14);
        p.MontasjestartDato ??= p.TilbudsfristDato.Value > idag ? p.TilbudsfristDato.Value.AddDays(21) : FremdriftsplanBeregner.Mandag(idag);
        p.OverleveringsfristDato ??= p.MontasjestartDato.Value.AddDays(56);

        // Lages planen på et prosjekt som er i gang, regnes det som allerede
        // er passert som avsluttet. Det kan endres per oppgave etterpå.
        var faser = FremdriftsplanBeregner.Faser(FremdriftsplanBeregner.FristerFor(p)!);
        var rekkefolge = 0;
        foreach (var m in FremdriftsplanBeregner.Mal)
        {
            var passert = FremdriftsplanBeregner.AktivitetDatoer(faser[m.Fase], m.DelFra, m.DelTil).Til < idag;
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
}

public static class ProsjektplanTekst
{
    public static readonly System.Globalization.CultureInfo Kultur = System.Globalization.CultureInfo.GetCultureInfo("nb-NO");
}
