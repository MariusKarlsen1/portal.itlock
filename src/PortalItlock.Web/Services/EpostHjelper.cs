using System.Globalization;

namespace PortalItlock.Web.Services;

// To ting gjør e-postsammenligning vanskeligere enn den ser ut:
//
// 1) SQLite sin innebygde LOWER()-funksjon kjenner kun ASCII a-z - bokstaver
//    som æ/ø/å lar den stå helt urørt, så et vanlig
//    "b.Epost.ToLower() == x.ToLower()"-filter oversatt til SQL kan feile for
//    slike e-poster selv om de er visuelt identiske.
//
// 2) Nettlesere punycode-koder automatisk internasjonale domener i
//    <input type="email">-felt FØR verdien sendes inn (f.eks.
//    "test@testfirmaås.no" -> "test@xn--testfirmas-95a.no") - men ikke i
//    vanlige tekstfelt, som admin-opprettelsesskjemaet i plattform-adminen
//    bruker for å registrere den første admin-brukerens e-post. Samme e-post
//    kan derfor ligge i databasen skrevet med bokstavene, men komme inn fra
//    innloggings-/passordsidene i punycode-form - uten normalisering
//    "finnes" ikke brukeren lenger, uten noen synlig feil.
//
// All sammenligning skjer derfor i .NET (etter at radene er hentet ut), med
// domenedelen kjørt gjennom IdnMapping for å normalisere til samme form.
public static class EpostHjelper
{
    private static readonly IdnMapping Idn = new();

    public static bool ErLik(string? a, string? b) =>
        string.Equals(Normaliser(a), Normaliser(b), StringComparison.OrdinalIgnoreCase);

    private static string? Normaliser(string? epost)
    {
        if (string.IsNullOrWhiteSpace(epost))
        {
            return epost?.Trim();
        }

        var trimmet = epost.Trim();
        var alfakrollIndeks = trimmet.LastIndexOf('@');
        if (alfakrollIndeks < 0 || alfakrollIndeks == trimmet.Length - 1)
        {
            return trimmet;
        }

        var lokalDel = trimmet[..alfakrollIndeks];
        var domene = trimmet[(alfakrollIndeks + 1)..];

        try
        {
            domene = Idn.GetAscii(domene);
        }
        catch (ArgumentException)
        {
            // Ugyldig IDN-domene (f.eks. tomt segment) - behold originalen,
            // sammenligningen faller da tilbake til ren tekstlikhet.
        }

        return $"{lokalDel}@{domene}";
    }
}
