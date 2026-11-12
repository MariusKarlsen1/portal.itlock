using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Regner ut fakturerbar sum og kost for en arbeidsordre - fra alle tilbudene
// på ordren hvis den har tilbud (hovedtilbudet + ekstra tilbud), ellers fra
// varer/montasjepris lagt inn direkte på ordren. Summen for et tilbud er den
// som ble låst da tilbudet ble sendt/godkjent (SendtSum) - samme tall som i
// Tilbudsreserve - og ellers regnet eks. mva som på tilbudet.
// Krever at Tilbud og EkstraTilbud.Tilbud (med linjer) er lastet.
public static class ArbeidsordreBeregningHelper
{
    public static List<Tilbud> AlleTilbud(Arbeidsordre a) =>
        (a.Tilbud is null ? new List<Tilbud>() : [a.Tilbud])
            .Concat(a.EkstraTilbud.Select(x => x.Tilbud).OfType<Tilbud>())
            .DistinctBy(t => t.Id)
            .ToList();

    public static decimal BeregnVareSum(Arbeidsordre a)
    {
        var tilbud = AlleTilbud(a);
        if (tilbud.Count > 0)
        {
            return tilbud.Sum(t => t.SendtSum ?? TilbudBeregningHelper.BeregnTotaltUtenMva(t));
        }

        var vareSum = a.Varer.Sum(v => v.Utpris * v.Antall);
        var montasjeSum = (a.Timepris ?? 0) * a.Timeregistreringer.Sum(t => t.TotalTimer);
        // Kjøring og tillegg montøren har ført på kodene (krever KodeLinjer lastet).
        return vareSum + montasjeSum + a.KodeLinjer.Sum(k => k.Antall * k.Salgspris);
    }

    public static decimal BeregnVareKost(Arbeidsordre a)
    {
        var tilbud = AlleTilbud(a);
        if (tilbud.Count > 0)
        {
            return tilbud.Sum(TilbudBeregningHelper.BeregnTotalKost);
        }

        var vareKost = a.Varer.Sum(v => v.Kostpris * v.Antall);
        var montasjeKost = (a.KostprisTime ?? 0) * a.Timeregistreringer.Sum(t => t.TotalTimer);
        return vareKost + montasjeKost + a.KodeLinjer.Sum(k => k.Antall * k.Kostpris);
    }
}
