namespace PortalItlock.Web.Models;

// E-postvarsler til brukerne i en organisasjon. Hver varseltype har roller som
// kan få den, og roller som får den som standard. I adminportalen kan
// standarden endres per organisasjon og rolle (TenantVarselRolle), og per
// bruker (TenantBrukerVarsel). Mangler en rad, gjelder standarden.
public static class EpostVarsler
{
    public const string OrdreTildelt = "ordre-tildelt";
    public const string ForesporselNy = "foresporsel-ny";
    public const string TilbudOppfolging = "tilbud-oppfolging";

    public sealed record Definisjon(string Nokkel, string Navn, string Beskrivelse, BrukerRolle[] Kan, BrukerRolle[] Standard);

    public static readonly Definisjon[] Alle =
    [
        new(OrdreTildelt, "Ny arbeidsordre tildelt meg", "Når en arbeidsordre settes på brukeren",
            [BrukerRolle.Admin, BrukerRolle.Prosjektleder, BrukerRolle.Montor],
            [BrukerRolle.Prosjektleder, BrukerRolle.Montor]),
        new(ForesporselNy, "Ny forespørsel", "Når det kommer en forespørsel på e-post, fra kundeportalen eller driftsportalen",
            [BrukerRolle.Admin, BrukerRolle.Prosjektleder],
            [BrukerRolle.Admin, BrukerRolle.Prosjektleder]),
        new(TilbudOppfolging, "Følg opp tilbud", "Påminnelse et valgt antall dager etter at et tilbud er sendt",
            [BrukerRolle.Admin, BrukerRolle.Prosjektleder],
            [BrukerRolle.Admin, BrukerRolle.Prosjektleder]),
    ];

    public static Definisjon? Finn(string nokkel) => Alle.FirstOrDefault(d => d.Nokkel == nokkel);

    // Får brukeren denne varselet? Brukerens eget valg går foran rollens.
    public static bool Gjelder(string nokkel, BrukerRolle rolle, int brukerId, IEnumerable<TenantVarselRolle> roller, IEnumerable<TenantBrukerVarsel> brukere)
    {
        if (Finn(nokkel) is not { } def || !def.Kan.Contains(rolle))
        {
            return false;
        }

        if (brukere.FirstOrDefault(b => b.BrukerId == brukerId && b.Nokkel == nokkel) is { } egen)
        {
            return egen.Aktiv;
        }

        return RolleAktiv(nokkel, rolle, roller);
    }

    public static bool RolleAktiv(string nokkel, BrukerRolle rolle, IEnumerable<TenantVarselRolle> roller)
    {
        if (Finn(nokkel) is not { } def || !def.Kan.Contains(rolle))
        {
            return false;
        }

        return roller.FirstOrDefault(r => r.Nokkel == nokkel && r.Rolle == rolle) is { } valg
            ? valg.Aktiv
            : def.Standard.Contains(rolle);
    }
}

// Organisasjonens valg for en rolle. Mangler raden, gjelder standarden.
public class TenantVarselRolle
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Nokkel { get; set; } = "";
    public BrukerRolle Rolle { get; set; }
    public bool Aktiv { get; set; }
}

// En brukers eget valg, som går foran rollens.
public class TenantBrukerVarsel
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int BrukerId { get; set; }
    public string Nokkel { get; set; } = "";
    public bool Aktiv { get; set; }
}
