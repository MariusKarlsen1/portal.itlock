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
        var montasjekost = tilbud.Montasjekost ?? kalkulertMontasjekost;

        return utprisVarer + montasjekost;
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
