namespace PortalItlock.Web.Services;

// Norske røde dager - faste datoer og bevegelige helligdager utledet fra 1. påskedag
// (Gauss' påskeformel). Rent beregnet, ingen database nødvendig.
public static class NorskHelligdag
{
    public static bool ErSondag(DateTime dato) => dato.DayOfWeek == DayOfWeek.Sunday;

    public static string? Navn(DateTime dato)
    {
        var d = dato.Date;
        var paske = ForsteParskedag(d.Year);

        if (d == new DateTime(d.Year, 1, 1)) return "Nyttårsdag";
        if (d == paske.AddDays(-3)) return "Skjærtorsdag";
        if (d == paske.AddDays(-2)) return "Langfredag";
        if (d == paske) return "1. påskedag";
        if (d == paske.AddDays(1)) return "2. påskedag";
        if (d == new DateTime(d.Year, 5, 1)) return "Arbeidernes dag";
        if (d == paske.AddDays(39)) return "Kristi himmelfart";
        if (d == new DateTime(d.Year, 5, 17)) return "Grunnlovsdagen";
        if (d == paske.AddDays(49)) return "1. pinsedag";
        if (d == paske.AddDays(50)) return "2. pinsedag";
        if (d == new DateTime(d.Year, 12, 25)) return "1. juledag";
        if (d == new DateTime(d.Year, 12, 26)) return "2. juledag";

        return null;
    }

    private static DateTime ForsteParskedag(int ar)
    {
        var a = ar % 19;
        var b = ar / 100;
        var c = ar % 100;
        var e = b / 4;
        var f = b % 4;
        var g = (8 * b + 13) / 25;
        var h = (19 * a + b - e - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (2 * f + 2 * i - h - k + 32) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var maned = (h + l - 7 * m + 114) / 31;
        var dag = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(ar, maned, dag);
    }
}
