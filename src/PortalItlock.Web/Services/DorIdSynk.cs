using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Feltene som følger en Dør ID (DorIdMal) over til dørene: dørkonstruksjon,
// fløyer, bredde, høyde, brann, lyd og fri bredde - med samme navn begge steder.
// Endres Dør ID-en, oppdateres bare dørene der feltet fortsatt har den gamle
// verdien; felt som er endret på selve døren, blir stående.
public static class DorIdSynk
{
    public static readonly string[] Felt = ["konstruksjon", "floyer", "bredde", "hoyde", "brann", "lyd", "fribredde"];

    // Verdiene på Dør ID-en, som tekst per felt (for å kunne sammenligne før og etter en endring).
    public static Dictionary<string, string?> Verdier(DorIdMal m) => new()
    {
        ["konstruksjon"] = Tom(m.Dorkonstruksjon),
        ["floyer"] = m.ToFloyet?.ToString(),
        ["bredde"] = m.Bredde?.ToString(),
        ["hoyde"] = m.Hoyde?.ToString(),
        ["brann"] = Tom(m.Brann),
        ["lyd"] = Tom(m.Lyd),
        ["fribredde"] = m.FriBredde086?.ToString()
    };

    public static string? Verdi(Dor d, string felt) => felt switch
    {
        "konstruksjon" => Tom(d.Dortype),
        "floyer" => d.ToFloyet.ToString(),
        "bredde" => d.Bredde?.ToString(),
        "hoyde" => d.Hoyde?.ToString(),
        "brann" => Tom(d.Brann),
        "lyd" => Tom(d.Lyd),
        "fribredde" => d.FriBredde086?.ToString(),
        _ => null
    };

    // Feltet følger Dør ID-en når Dør ID-en har en verdi og døren har den samme.
    public static bool Folger(DorIdMal m, Dor d, string felt) =>
        Verdier(m)[felt] is { } v && v == Verdi(d, felt);

    // Kopierer alle feltene Dør ID-en har verdi i, til døren (når Dør ID-en tas i bruk).
    public static void Kopier(DorIdMal m, Dor d)
    {
        foreach (var felt in Felt)
        {
            Sett(m, d, felt);
        }

        d.SynkBxH();
    }

    // Etter en endring på Dør ID-en: oppdater feltene på dørene som hadde den gamle verdien.
    public static int Oppdater(DorIdMal m, Dictionary<string, string?> forrige, IEnumerable<Dor> dorer)
    {
        var na = Verdier(m);
        var endret = Felt.Where(f => na[f] != forrige.GetValueOrDefault(f)).ToList();
        var antall = 0;
        foreach (var d in dorer.Where(d => d.DorIdMalId == m.Id))
        {
            var berort = false;
            foreach (var felt in endret)
            {
                var gammel = forrige.GetValueOrDefault(felt);
                // Felt som er endret på døren (ulik den gamle Dør ID-verdien) røres ikke.
                if (gammel is null || Verdi(d, felt) == gammel)
                {
                    Sett(m, d, felt);
                    berort = true;
                }
            }

            if (berort)
            {
                d.SynkBxH();
                antall++;
            }
        }

        return antall;
    }

    private static void Sett(DorIdMal m, Dor d, string felt)
    {
        switch (felt)
        {
            case "konstruksjon" when !string.IsNullOrWhiteSpace(m.Dorkonstruksjon): d.Dortype = m.Dorkonstruksjon!.Trim(); break;
            case "floyer" when m.ToFloyet is { } to: d.ToFloyet = to; break;
            case "bredde" when m.Bredde is { } b: d.Bredde = b; break;
            case "hoyde" when m.Hoyde is { } h: d.Hoyde = h; break;
            case "brann" when !string.IsNullOrWhiteSpace(m.Brann): d.Brann = m.Brann!.Trim(); break;
            case "lyd" when !string.IsNullOrWhiteSpace(m.Lyd): d.Lyd = m.Lyd!.Trim(); break;
            case "fribredde" when m.FriBredde086 is { } fb: d.FriBredde086 = fb; break;
        }
    }

    private static string? Tom(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
