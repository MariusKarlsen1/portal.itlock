using System.Globalization;

namespace PortalItlock.Web.Services;

public enum RapportPeriodeValg
{
    DenneMaaned,
    ForrigeMaaned,
    HittilIAr,
    Ar,
    Egendefinert,
    Alle
}

// Felles periodevalg for alle rapportene under /rapporter: hurtigvalg, valg
// av hele år og egne datoer, pluss sammenligning med samme periode året før.
public sealed class RapportPeriode
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public RapportPeriode(RapportPeriodeValg standard = RapportPeriodeValg.HittilIAr)
    {
        Sett(standard);
    }

    public RapportPeriodeValg Valg { get; private set; }
    public int Ar { get; private set; } = DateTime.Today.Year;
    public DateTime Fra { get; set; }
    public DateTime Til { get; set; }
    public bool Sammenlign { get; set; } = true;

    public bool ErAlle => Valg == RapportPeriodeValg.Alle;

    // Samme periode ett år tilbake.
    public DateTime SmlFra => Fra.AddYears(-1);
    public DateTime SmlTil => Til.AddYears(-1);

    public static IEnumerable<int> ValgbareAr(int antall = 6) =>
        Enumerable.Range(0, antall).Select(i => DateTime.Today.Year - i);

    public void Sett(RapportPeriodeValg valg, int? ar = null)
    {
        var idag = DateTime.Today;
        Valg = valg;
        switch (valg)
        {
            case RapportPeriodeValg.DenneMaaned:
                Fra = new DateTime(idag.Year, idag.Month, 1);
                Til = idag;
                break;
            case RapportPeriodeValg.ForrigeMaaned:
                Fra = new DateTime(idag.Year, idag.Month, 1).AddMonths(-1);
                Til = Fra.AddMonths(1).AddDays(-1);
                break;
            case RapportPeriodeValg.HittilIAr:
                Fra = new DateTime(idag.Year, 1, 1);
                Til = idag;
                break;
            case RapportPeriodeValg.Ar:
                Ar = ar ?? Ar;
                Fra = new DateTime(Ar, 1, 1);
                Til = Ar == idag.Year ? idag : new DateTime(Ar, 12, 31);
                break;
            case RapportPeriodeValg.Alle:
                Fra = new DateTime(2000, 1, 1);
                Til = idag;
                break;
            case RapportPeriodeValg.Egendefinert:
                break;
        }

        if (valg != RapportPeriodeValg.Ar)
        {
            Ar = Fra.Year;
        }
    }

    public bool Inneholder(DateTime dato) => dato.Date >= Fra.Date && dato.Date <= Til.Date;

    public string Tekst => ErAlle
        ? "Alle datoer"
        : $"{Fra.ToString("dd.MM.yyyy", Kultur)} – {Til.ToString("dd.MM.yyyy", Kultur)}";

    public string SmlTekst => Valg switch
    {
        RapportPeriodeValg.Ar => (Ar - 1).ToString(Kultur),
        RapportPeriodeValg.HittilIAr => "samme periode i fjor",
        _ => "samme periode i fjor"
    };

    public static string Kr(decimal belop) => belop.ToString("N0", Kultur) + " kr";

    public static string Tall(decimal verdi) => verdi.ToString("N0", Kultur);

    public static string Prosent(decimal del, decimal total) =>
        total == 0 ? "0 %" : (del / total * 100).ToString("0.0", Kultur) + " %";
}
