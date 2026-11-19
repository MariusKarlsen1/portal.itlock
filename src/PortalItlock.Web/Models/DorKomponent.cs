namespace PortalItlock.Web.Models;

public class DorKomponent
{
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int Antall { get; set; } = 1;
    public string? Enhet { get; set; }
    public DorKomponentPlassering? Plassering { get; set; }
    public LevertAv LevertAv { get; set; } = LevertAv.F;

    public bool Montert { get; set; }

    // Opsjon: varen står under «Opsjon beslag» på døren. Den er ikke med i
    // vanlige tilbud, plukk, lager eller montering - bare i opsjonstilbud
    // (TilbudType.Opsjon). Godtar kunden opsjonen, flyttes den til beslag.
    public bool ErOpsjon { get; set; }

    // Bare beslag vi leverer (F) monteres og hakes av av oss. Ld/Le leveres
    // komplett av dørprodusent/elektro. Ikke lagret - regnes fra LevertAv.
    public bool MonteresAvOss => LevertAv == LevertAv.F && !ErOpsjon;

    // Antall i beslagstellingen (montert x av y). Varer som telles i meter
    // (f.eks. kabel) er ett beslag uansett hvor mange meter. Ikke lagret.
    public int BeslagAntall => ErMetervare(Enhet ?? Component?.Enhet) ? 1 : Antall;

    public static bool ErMetervare(string? enhet) =>
        (enhet ?? "").Trim().TrimEnd('.').ToLowerInvariant() is "m" or "meter" or "lm" or "lnm" or "m1" or "mtr";
    public DateTime? MontertDato { get; set; }

    public int? MontertAvBrukerId { get; set; }
    public Bruker? MontertAvBruker { get; set; }
}

// Varene på en dør står alltid i samme rekkefølge: først det dørprodusenten (Ld)
// og elektro (Le) leverer, så våre varer etter komponenttype (rekkefølgen i
// komponentregisteret, f.eks. automatikk før albuebrytere), og opsjoner nederst.
public static class DorKomponentRekkefolge
{
    private static readonly StringComparer Navn = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("nb-NO"), true);

    public static IEnumerable<DorKomponent> IFastRekkefolge(this IEnumerable<DorKomponent> varer) => varer
        .OrderBy(k => k.ErOpsjon)
        .ThenBy(k => k.LevertAv switch { LevertAv.Ld => 0, LevertAv.Le => 1, _ => 2 })
        .ThenBy(k => k.Component?.Type?.Sortering ?? int.MaxValue)
        .ThenBy(k => k.Component?.Type is null ? 1 : 0)
        .ThenBy(k => k.Component?.Type?.Navn ?? "", Navn)
        .ThenBy(k => k.Component?.Navn ?? "", Navn);
}
