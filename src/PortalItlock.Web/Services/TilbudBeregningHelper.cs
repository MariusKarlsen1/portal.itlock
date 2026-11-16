using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Samme formel som TilbudSkjema.razor bruker for "Totalt uten mva" - hold synkronisert
// hvis beregningen der endres.
public static class TilbudBeregningHelper
{
    public static decimal BeregnTotaltUtenMva(Tilbud tilbud)
    {
        var utprisVarer = tilbud.Linjer
            .Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering)
            .Sum(l => l.Utpris * l.Antall);

        var estimertArbeidstidTimerMedRigg = (tilbud.EstimertTimerOverride ?? 0) * (1 + (tilbud.RiggDriftProsent ?? 0) / 100m);
        var kalkulertMontasjekost = Math.Round(tilbud.Timepris * estimertArbeidstidTimerMedRigg, 2);
        var montasjekost = tilbud.BrukerKodelinjer
            ? KodeLinjeBeregner.Totalt(tilbud.KodeLinjer, utprisVarer).Salg
            : tilbud.Montasjekost ?? kalkulertMontasjekost;

        return utprisVarer + montasjekost;
    }

    public sealed record DorPris(decimal Varer, decimal Montasje)
    {
        public decimal Total => Varer + Montasje;
    }

    // Opsjonstilbud per dør: varene på døren pluss dørens andel av montasjen.
    // Montasjen fordeles etter minuttene (minutter × antall) på varene, så
    // summen over dørene er den samme som montasjen i tilbudet.
    public static Dictionary<int, DorPris> PrisPerDor(Tilbud tilbud, Func<TilbudLinje, int> minutter)
    {
        var vareLinjer = tilbud.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering).ToList();
        var montasje = BeregnTotaltUtenMva(tilbud) - vareLinjer.Sum(l => l.Utpris * l.Antall);
        var alleMinutter = (decimal)vareLinjer.Sum(l => minutter(l) * l.Antall);

        return vareLinjer.Where(l => l.DorId is not null)
            .GroupBy(l => l.DorId!.Value)
            .ToDictionary(g => g.Key, g =>
            {
                var dorMinutter = g.Sum(l => minutter(l) * l.Antall);
                var andel = alleMinutter > 0 ? Math.Round(montasje * dorMinutter / alleMinutter, 2) : 0;
                return new DorPris(g.Sum(l => l.Utpris * l.Antall), andel);
            });
    }

    public static decimal BeregnTotalKost(Tilbud tilbud)
    {
        var varekost = tilbud.Linjer
            .Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering)
            .Sum(l => l.Innpris * (1 - l.EkstraRabattInnProsent / 100m) * l.Antall);

        var estimertArbeidstidTimerMedRigg = (tilbud.EstimertTimerOverride ?? 0) * (1 + (tilbud.RiggDriftProsent ?? 0) / 100m);
        var montasjeInnpris = (tilbud.MontasjeInnpris ?? 0) * estimertArbeidstidTimerMedRigg;

        return varekost + montasjeInnpris;
    }
}
