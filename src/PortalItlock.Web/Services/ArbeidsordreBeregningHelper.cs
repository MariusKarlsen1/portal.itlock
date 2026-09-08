using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Regner ut fakturerbar sum og kost for en arbeidsordre - fra det tilknyttede
// tilbudet hvis det finnes, ellers fra varer/montasjepris lagt inn direkte på ordren.
public static class ArbeidsordreBeregningHelper
{
    public static decimal BeregnVareSum(Arbeidsordre a)
    {
        if (a.Tilbud is not null)
        {
            return TilbudBeregningHelper.BeregnTotaltUtenMva(a.Tilbud);
        }

        var vareSum = a.Varer.Sum(v => v.Utpris * v.Antall);
        var montasjeSum = (a.Timepris ?? 0) * a.Timeregistreringer.Sum(t => t.TotalTimer);
        return vareSum + montasjeSum;
    }

    public static decimal BeregnVareKost(Arbeidsordre a)
    {
        if (a.Tilbud is not null)
        {
            return TilbudBeregningHelper.BeregnTotalKost(a.Tilbud);
        }

        var vareKost = a.Varer.Sum(v => v.Kostpris * v.Antall);
        var montasjeKost = (a.KostprisTime ?? 0) * a.Timeregistreringer.Sum(t => t.TotalTimer);
        return vareKost + montasjeKost;
    }
}
