using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Summer for kodelinjene på et tilbud (arbeid, kjøring, småmateriell osv.).
// Linjer i kroner er antall × pris. Prosentlinjer regnes av varenes utpris
// (ProsentVarer) eller av arbeidslinjene (ProsentArbeid), og antallet brukes
// ikke for dem.
//
// Synk skriver totalene til tilbudets gamle felt for montasje, så PDF,
// totaler, dekningsbidrag, Prosjektøkonomi og Tripletex virker uendret:
//   EstimertTimerOverride = timer på arbeidslinjene
//   Montasjekost          = salgssum for alle kodelinjene
//   MontasjeInnpris       = kostnad for alle kodelinjene per time
//   Timepris              = snitt salgspris per arbeidstime
public static class KodeLinjeBeregner
{
    public sealed record Linjesum(decimal Kost, decimal Salg);

    public sealed record Totaler(decimal Timer, decimal Kost, decimal Salg, decimal TimerSalg);

    public static Linjesum Sum(TilbudKodeLinje l, IReadOnlyCollection<TilbudKodeLinje> alle, decimal utprisVarer)
    {
        if (!l.Enhet.ErProsent())
        {
            return new(Math.Round(l.Antall * l.Kostpris, 2), Math.Round(l.Antall * l.Salgspris, 2));
        }

        var arbeid = alle.Where(x => x.Type == PrisKodeType.Arbeid && !x.Enhet.ErProsent()).ToList();
        var (kostGrunnlag, salgGrunnlag) = l.Enhet == PrisKodeEnhet.ProsentVarer
            ? (utprisVarer, utprisVarer)
            : (arbeid.Sum(x => x.Antall * x.Kostpris), arbeid.Sum(x => x.Antall * x.Salgspris));
        return new(Math.Round(kostGrunnlag * l.Kostpris / 100m, 2), Math.Round(salgGrunnlag * l.Salgspris / 100m, 2));
    }

    public static Totaler Totalt(IReadOnlyCollection<TilbudKodeLinje> linjer, decimal utprisVarer)
    {
        var summer = linjer.Select(l => (Linje: l, Sum: Sum(l, linjer, utprisVarer))).ToList();
        var timelinjer = summer.Where(x => x.Linje.Type == PrisKodeType.Arbeid && x.Linje.Enhet == PrisKodeEnhet.Time).ToList();
        return new(
            timelinjer.Sum(x => x.Linje.Antall),
            summer.Sum(x => x.Sum.Kost),
            summer.Sum(x => x.Sum.Salg),
            timelinjer.Sum(x => x.Sum.Salg));
    }

    public static void Synk(Tilbud t, decimal utprisVarer)
    {
        if (!t.BrukerKodelinjer)
        {
            return;
        }

        var tot = Totalt(t.KodeLinjer, utprisVarer);
        t.RiggDriftProsent = 0;
        t.EstimertTimerOverride = tot.Timer;
        t.Montasjekost = tot.Salg;
        t.Timepris = tot.Timer > 0 ? Math.Round(tot.TimerSalg / tot.Timer, 2) : 0;
        t.MontasjeInnpris = tot.Timer > 0 ? Math.Round(tot.Kost / tot.Timer, 4) : 0;
    }

    // Standardkodene en ny organisasjon starter med. Prisene er eksempler og
    // endres under Priskoder, eller hentes fra Tripletex.
    public static List<PrisKode> Standardkoder(decimal timekost, decimal timepris) =>
    [
        new() { Kode = "100", Navn = "Montasje", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, Kostpris = timekost, Salgspris = timepris, ErMontasje = true, Rekkefolge = 0 },
        new() { Kode = "110", Navn = "Montasje, overtid 50 %", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, Kostpris = Math.Round(timekost * 1.35m), Salgspris = Math.Round(timepris * 1.5m), Rekkefolge = 1 },
        new() { Kode = "120", Navn = "Programmering", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, Kostpris = timekost, Salgspris = timepris, Rekkefolge = 2 },
        new() { Kode = "130", Navn = "Prosjektledelse", Type = PrisKodeType.Arbeid, Enhet = PrisKodeEnhet.Time, Kostpris = timekost, Salgspris = timepris, Rekkefolge = 3 },
        new() { Kode = "200", Navn = "Kjøring", Beskrivelse = "Per kilometer", Type = PrisKodeType.Kjoring, Enhet = PrisKodeEnhet.Km, Kostpris = 4.5m, Salgspris = 9m, Rekkefolge = 4 },
        new() { Kode = "210", Navn = "Utkjøringstillegg", Beskrivelse = "Fast per oppmøte", Type = PrisKodeType.Kjoring, Enhet = PrisKodeEnhet.Stk, Kostpris = 0, Salgspris = 490, Rekkefolge = 5 },
        new() { Kode = "300", Navn = "Småmateriell", Beskrivelse = "Skruer, plugger, lim", Type = PrisKodeType.Materiell, Enhet = PrisKodeEnhet.ProsentVarer, Kostpris = 2, Salgspris = 4, Rekkefolge = 6 },
        new() { Kode = "400", Navn = "Rigg og drift", Type = PrisKodeType.Tillegg, Enhet = PrisKodeEnhet.ProsentArbeid, Kostpris = 0, Salgspris = 5, Rekkefolge = 7 },
    ];

    public static TilbudKodeLinje LinjeFra(PrisKode k, decimal antall = 1) => new()
    {
        PrisKodeId = k.Id,
        Kode = k.Kode,
        Navn = k.Navn,
        Beskrivelse = k.Beskrivelse,
        Type = k.Type,
        Enhet = k.Enhet,
        Antall = k.Enhet.ErProsent() ? 1 : antall,
        Kostpris = k.Kostpris,
        Salgspris = k.Salgspris
    };
}
