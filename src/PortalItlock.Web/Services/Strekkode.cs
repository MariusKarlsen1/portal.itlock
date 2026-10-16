using System.Globalization;
using System.Text;

namespace PortalItlock.Web.Services;

// Strekkoder som SVG for etiketter: EAN-13 / EAN-8 når GTIN er gyldig (12-, 13-
// eller 8-sifret med riktig kontrollsiffer), ellers Code 128 (B) av teksten.
// Skrevet for hånd så vi slipper en egen pakke kun for dette.
public static class Strekkode
{
    // Modulmønstre for EAN: L (odde), G (like) og R (høyre side).
    private static readonly string[] L = ["0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011"];
    private static readonly string[] G = ["0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111"];
    private static readonly string[] R = ["1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100"];
    private static readonly string[] Paritet = ["LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"];

    // Code 128: bredder (strek, rom, strek, rom, strek, rom) for verdi 0-105, og stopp.
    private static readonly string[] Code128 =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232"
    ];
    private const string Code128Stopp = "2331112";

    /// <summary>Normalisert GTIN (kun sifre) hvis den er en gyldig EAN-13/UPC-A/EAN-8, ellers null.</summary>
    public static string? GyldigEan(string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
        {
            return null;
        }

        var sifre = new string(gtin.Where(char.IsDigit).ToArray());
        if (sifre.Length == 12)
        {
            sifre = "0" + sifre; // UPC-A er en EAN-13 med 0 foran
        }
        if (sifre.Length == 14 && sifre[0] == '0')
        {
            sifre = sifre[1..]; // GTIN-14 med 0 foran er samme vare
        }
        if (sifre.Length is not (13 or 8))
        {
            return null;
        }

        return Kontrollsiffer(sifre[..^1]) == sifre[^1] - '0' ? sifre : null;
    }

    /// <summary>Om varen får EAN-strekkode (fra GTIN, eller fra produktkoden når GTIN mangler).</summary>
    public static bool HarEan(string? gtin, string? reserve) =>
        GyldigEan(gtin) is not null || (string.IsNullOrWhiteSpace(gtin) && GyldigEan(reserve) is not null);

    private static int Kontrollsiffer(string uten)
    {
        var sum = 0;
        for (var i = 0; i < uten.Length; i++)
        {
            var siffer = uten[uten.Length - 1 - i] - '0';
            sum += i % 2 == 0 ? siffer * 3 : siffer;
        }
        return (10 - sum % 10) % 10;
    }

    /// <summary>
    /// Lager SVG med strekkode og teksten under. Bruker EAN når GTIN er gyldig,
    /// ellers Code 128 av <paramref name="reserve"/> (f.eks. GTIN som ikke er EAN, eller produktkode).
    /// Returnerer null når det ikke finnes noe å lage strekkode av.
    /// </summary>
    public static string? Svg(string? gtin, string? reserve = null)
    {
        // Mangler GTIN, men produktkoden er en gyldig EAN, brukes den.
        var ean = GyldigEan(gtin) ?? (string.IsNullOrWhiteSpace(gtin) ? GyldigEan(reserve) : null);
        if (ean is not null)
        {
            return ean.Length == 13 ? Ean13(ean) : Ean8(ean);
        }

        var tekst = (string.IsNullOrWhiteSpace(gtin) ? reserve : gtin)?.Trim();
        return string.IsNullOrEmpty(tekst) ? null : Code128B(tekst);
    }

    private static string Ean13(string s)
    {
        var moduler = new StringBuilder("101");
        var paritet = Paritet[s[0] - '0'];
        for (var i = 1; i <= 6; i++)
        {
            moduler.Append(paritet[i - 1] == 'L' ? L[s[i] - '0'] : G[s[i] - '0']);
        }
        moduler.Append("01010");
        for (var i = 7; i <= 12; i++)
        {
            moduler.Append(R[s[i] - '0']);
        }
        moduler.Append("101");

        // 95 moduler + 9 i stille sone til venstre (der første siffer står) og 7 til høyre.
        var tekster = new List<(double X, string T)> { (4, s[..1]), (9 + 3 + 21, s[1..7]), (9 + 50 + 21, s[7..]) };
        return Tegn(moduler.ToString(), 9, 7, tekster, vakter: [0, 1, 2, 45, 46, 47, 48, 49, 92, 93, 94]);
    }

    private static string Ean8(string s)
    {
        var moduler = new StringBuilder("101");
        for (var i = 0; i < 4; i++)
        {
            moduler.Append(L[s[i] - '0']);
        }
        moduler.Append("01010");
        for (var i = 4; i < 8; i++)
        {
            moduler.Append(R[s[i] - '0']);
        }
        moduler.Append("101");

        var tekster = new List<(double X, string T)> { (7 + 3 + 14, s[..4]), (7 + 36 + 14, s[4..]) };
        return Tegn(moduler.ToString(), 7, 7, tekster, vakter: [0, 1, 2, 31, 32, 33, 34, 35, 64, 65, 66]);
    }

    private static string Code128B(string tekst)
    {
        // Kun tegn som finnes i sett B (ASCII 32-127); andre byttes med '?'.
        var verdier = new List<int> { 104 };
        foreach (var t in tekst)
        {
            verdier.Add(t is >= ' ' and <= '\u007f' ? t - 32 : '?' - 32);
        }
        var sjekk = verdier[0];
        for (var i = 1; i < verdier.Count; i++)
        {
            sjekk += verdier[i] * i;
        }
        verdier.Add(sjekk % 103);

        var moduler = new StringBuilder();
        foreach (var bredder in verdier.Select(v => Code128[v]).Append(Code128Stopp))
        {
            for (var i = 0; i < bredder.Length; i++)
            {
                moduler.Append(i % 2 == 0 ? '1' : '0', bredder[i] - '0');
            }
        }

        var lengde = moduler.Length;
        return Tegn(moduler.ToString(), 10, 10, [(10 + lengde / 2.0, tekst)], vakter: []);
    }

    // Tegner modulene som svarte rektangler. Vaktstrekene (EAN) går litt lenger ned.
    private static string Tegn(string moduler, int venstre, int hoyre, List<(double X, string T)> tekster, int[] vakter)
    {
        const double strekHoyde = 30;
        const double vaktHoyde = 35;
        const double tekstStr = 9;
        var bredde = venstre + moduler.Length + hoyre;
        var hoyde = vaktHoyde + tekstStr + 1;
        var vaktSett = vakter.ToHashSet();

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {bredde} {hoyde}' shape-rendering='crispEdges'>");
        sb.Append(CultureInfo.InvariantCulture, $"<rect width='{bredde}' height='{hoyde}' fill='#fff'/>");
        var i = 0;
        while (i < moduler.Length)
        {
            if (moduler[i] != '1')
            {
                i++;
                continue;
            }
            var start = i;
            while (i < moduler.Length && moduler[i] == '1')
            {
                i++;
            }
            var h = vaktSett.Contains(start) ? vaktHoyde : strekHoyde;
            sb.Append(CultureInfo.InvariantCulture, $"<rect x='{venstre + start}' y='0' width='{i - start}' height='{h}' fill='#000'/>");
        }
        foreach (var (x, t) in tekster)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x='{x.ToString("0.#", CultureInfo.InvariantCulture)}' y='{strekHoyde + tekstStr}' font-family='Arial, sans-serif' font-size='{tekstStr}' text-anchor='middle' fill='#000'>{System.Net.WebUtility.HtmlEncode(t)}</text>");
        }
        sb.Append("</svg>");
        return sb.ToString();
    }
}
