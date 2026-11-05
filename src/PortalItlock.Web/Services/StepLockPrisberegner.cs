namespace PortalItlock.Web.Services;

public enum StepLockTilkobling { PoE, Dce, Lte }

public enum StepLockAbonnement { Basic, Standard, Premium }

public sealed record StepLockValg(
    int Porttelefoner,
    int Svarapparater,
    int KortUtenPin,
    int KortMedPin,
    int Brikker,
    StepLockTilkobling PorttelefonTilkobling = StepLockTilkobling.PoE,
    StepLockTilkobling KortUtenPinTilkobling = StepLockTilkobling.PoE,
    StepLockTilkobling KortMedPinTilkobling = StepLockTilkobling.PoE);

public sealed record StepLockLinje(string Artikkel, int Antall, decimal Pris)
{
    public decimal Sum => Antall * Pris;
}

public sealed record StepLockResultat(
    IReadOnlyList<StepLockLinje> Linjer,
    StepLockAbonnement? Abonnement,
    decimal AbonnementPris,
    decimal Tillegg4G)
{
    public decimal Hardware => Linjer.Sum(l => l.Sum);
    public decimal PerManed => AbonnementPris + Tillegg4G;
    public decimal ForsteAr => Hardware + PerManed * 12;
}

// Regnestykket fra forhandlerarket "StepLock Access Priskonfigurator". Prisene
// kommer utenfra, slik at de alltid er de som står i komponentregisteret.
public static class StepLockPrisberegner
{
    public const string DorsentralPorttelefon = "ST2029-POE";
    public const string Porttelefon = "ST2020-S";
    public const string Svarenhet = "ST2022";
    public const string Kortleser = "ST2010";
    public const string Kodeleser = "ST2011";
    public const string Brikke = "ST2066-MF";
    public const string DorsentralPoe = "ST2042-POE";
    public const string DorsentralDce = "ST2042-DCE";
    public const string LteRouter = "ST241";
    public const string Kabel = "ST2063";
    public const string Veggboks = "ST2071";
    public const string Basic = "BASIC";
    public const string Standard = "STANDARD";
    public const string Premium = "PREMIUM";
    public const string LtePorttelefon = "M-001";
    public const string LteKortleser = "M-002";

    public static readonly string[] Hardwarekoder =
    [
        DorsentralPorttelefon, Porttelefon, Svarenhet, Kortleser, Kodeleser, Brikke,
        DorsentralPoe, DorsentralDce, LteRouter, Kabel, Veggboks
    ];

    public static readonly string[] AlleKoder =
        [.. Hardwarekoder, Basic, Standard, Premium, LtePorttelefon, LteKortleser];

    // Slik arket velger abonnement. Hullene er med vilje, de er like i arket:
    // 5 kortlesere uten porttelefon og 21+ porttelefoner med 5-15 kortlesere gir
    // ikke noe abonnement.
    public static StepLockAbonnement? VelgAbonnement(int porttelefoner, int kortlesere)
    {
        if (porttelefoner is >= 11 and <= 20 || kortlesere >= 16)
        {
            return StepLockAbonnement.Premium;
        }

        if (porttelefoner <= 10 && (porttelefoner >= 2 || kortlesere is >= 6 and <= 15))
        {
            return StepLockAbonnement.Standard;
        }

        return porttelefoner == 1 || kortlesere <= 4 ? StepLockAbonnement.Basic : null;
    }

    public static StepLockResultat Beregn(StepLockValg v, Func<string, decimal> pris)
    {
        var dce = (v.KortUtenPinTilkobling == StepLockTilkobling.Dce ? v.KortUtenPin : 0)
            + (v.KortMedPinTilkobling == StepLockTilkobling.Dce ? v.KortMedPin : 0);
        var kortLte = (v.KortUtenPinTilkobling == StepLockTilkobling.Lte ? v.KortUtenPin : 0)
            + (v.KortMedPinTilkobling == StepLockTilkobling.Lte ? v.KortMedPin : 0);
        var portLte = v.PorttelefonTilkobling == StepLockTilkobling.Lte ? v.Porttelefoner : 0;
        var kortlesere = v.KortUtenPin + v.KortMedPin;

        var antall = new Dictionary<string, int>
        {
            [DorsentralPorttelefon] = v.Porttelefoner,
            [Porttelefon] = v.Porttelefoner,
            [Svarenhet] = v.Svarapparater,
            [Kortleser] = v.KortUtenPin,
            [Kodeleser] = v.KortMedPin,
            [Brikke] = v.Brikker,
            [DorsentralPoe] = kortlesere - dce,
            [DorsentralDce] = dce,
            [LteRouter] = portLte + kortLte,
            [Kabel] = v.Porttelefoner,
            [Veggboks] = v.Porttelefoner
        };

        var abonnement = VelgAbonnement(v.Porttelefoner, kortlesere);
        var abonnementPris = abonnement switch
        {
            StepLockAbonnement.Basic => pris(Basic),
            StepLockAbonnement.Standard => pris(Standard),
            StepLockAbonnement.Premium => pris(Premium),
            _ => 0m
        };

        return new StepLockResultat(
            Hardwarekoder.Select(k => new StepLockLinje(k, antall[k], pris(k))).ToList(),
            abonnement,
            abonnementPris,
            portLte * pris(LtePorttelefon) + kortLte * pris(LteKortleser));
    }

    // Prisen for én enhet med alt som hører til, slik den vises på kortet.
    public static decimal PrisPorttelefon(StepLockTilkobling tilkobling, Func<string, decimal> pris) =>
        pris(DorsentralPorttelefon) + pris(Porttelefon) + pris(Kabel) + pris(Veggboks)
        + (tilkobling == StepLockTilkobling.Lte ? pris(LteRouter) : 0);

    public static decimal PrisKortleser(string leser, StepLockTilkobling tilkobling, Func<string, decimal> pris) =>
        pris(leser)
        + pris(tilkobling == StepLockTilkobling.Dce ? DorsentralDce : DorsentralPoe)
        + (tilkobling == StepLockTilkobling.Lte ? pris(LteRouter) : 0);
}
