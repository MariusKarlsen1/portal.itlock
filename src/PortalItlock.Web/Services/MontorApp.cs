using System.Globalization;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Felles hjelpere og timetyper for montørappen på mobil (/app/...).
public static class MontorApp
{
    public static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    // Normal arbeidsuke, brukt til ringen på Timeliste og som standard ved fravær.
    public const decimal Ukesmal = 37.5m;
    public const decimal Arbeidsdag = 7.5m;

    public enum TimetypeGruppe { Jobb, Intern, Fravar }

    // En timetype i «Legg til timer». Jobb og internt arbeid blir en
    // timeregistrering på en arbeidsordre; fravær blir en fraværssøknad.
    public sealed record Timetype(string Id, string Kode, string Navn, TimetypeGruppe Gruppe,
        int? PrisKodeId = null, TimeregistreringType Type = TimeregistreringType.NormalArbeidstid,
        TimeAktivitet Aktivitet = TimeAktivitet.Montering, FravarType? Fravar = null)
    {
        public string Visning => string.IsNullOrEmpty(Kode) ? Navn : $"{Kode} {Navn}";
        public bool Fakturerbar => Gruppe == TimetypeGruppe.Jobb;
    }

    public static string GruppeNavn(TimetypeGruppe g) => g switch
    {
        TimetypeGruppe.Jobb => "På en jobb",
        TimetypeGruppe.Intern => "Internt arbeid",
        _ => "Fravær"
    };

    // Timetypene: arbeidskodene (time) fra Prosjektøkonomi, ellers aktivitetene,
    // så internt arbeid og fraværstypene.
    public static List<Timetype> Timetyper(IEnumerable<PrisKode> koder)
    {
        var liste = koder
            .Where(k => k.Aktiv && k.Type == PrisKodeType.Arbeid && k.Enhet == PrisKodeEnhet.Time)
            .OrderBy(k => k.Rekkefolge).ThenBy(k => k.Kode)
            .Select(k => new Timetype($"k{k.Id}", k.Kode, k.Navn, TimetypeGruppe.Jobb, k.Id, k.Timetype, AktivitetFor(k.Navn)))
            .ToList();

        if (liste.Count == 0)
        {
            liste.AddRange(new[] { TimeAktivitet.Montering, TimeAktivitet.Programmering, TimeAktivitet.Service, TimeAktivitet.Befaring, TimeAktivitet.Kjoring }
                .Select(a => new Timetype($"a{a}", "", a.Visningsnavn(), TimetypeGruppe.Jobb, Aktivitet: a)));
        }

        liste.AddRange(new[] { TimeAktivitet.Intern, TimeAktivitet.Mote, TimeAktivitet.Dokumentasjon }
            .Select(a => new Timetype($"i{a}", "", a == TimeAktivitet.Intern ? "Internt arbeid" : a.Visningsnavn(), TimetypeGruppe.Intern, Aktivitet: a)));

        liste.AddRange(Enum.GetValues<FravarType>()
            .Select(f => new Timetype($"f{f}", "", f.Visningsnavn(), TimetypeGruppe.Fravar, Fravar: f)));
        return liste;
    }

    // Typen til en lagret timeregistrering, for visning i lista.
    public static Timetype TypeFor(Timeregistrering t, List<Timetype> typer) =>
        typer.FirstOrDefault(x => x.PrisKodeId is not null && x.PrisKodeId == t.PrisKodeId)
        ?? typer.FirstOrDefault(x => x.Gruppe != TimetypeGruppe.Fravar && x.PrisKodeId is null && x.Aktivitet == t.Aktivitet)
        ?? new Timetype("", "", t.Aktivitet.Visningsnavn(), TimetypeGruppe.Jobb, Aktivitet: t.Aktivitet);

    private static TimeAktivitet AktivitetFor(string navn) =>
        navn.Contains("program", StringComparison.OrdinalIgnoreCase) ? TimeAktivitet.Programmering
        : navn.Contains("service", StringComparison.OrdinalIgnoreCase) ? TimeAktivitet.Service
        : navn.Contains("kjør", StringComparison.OrdinalIgnoreCase) ? TimeAktivitet.Kjoring
        : navn.Contains("ledelse", StringComparison.OrdinalIgnoreCase) || navn.Contains("møte", StringComparison.OrdinalIgnoreCase) ? TimeAktivitet.Mote
        : TimeAktivitet.Montering;

    public static DateTime Mandag(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public static string Timer(decimal t) => t.ToString("0.##", Kultur);

    public static string Dato(DateTime d) => d.ToString("dd.MM.yyyy", Kultur);

    // Kort status og prikkfarge for et prosjekt i prosjektlista og på kartet.
    public static string ProsjektStatusKort(ProsjektStatus? s) => s switch
    {
        ProsjektStatus.UnderArbeid => "Under arbeid",
        ProsjektStatus.Aktiv => "Prosjektering",
        ProsjektStatus.Registrert => "Registrert",
        { } x => x.Visningsnavn(),
        _ => "Uten status"
    };

    public static string ProsjektStatusKlasse(ProsjektStatus? s) => s switch
    {
        ProsjektStatus.UnderArbeid => "pagaende",
        ProsjektStatus.Aktiv => "ny",
        _ => "klar"
    };

    public static string DagKort(DateTime d) => d.ToString("ddd", Kultur).TrimEnd('.') + ". " + d.ToString("dd.MM.yyyy", Kultur);

    public static string DagLang(DateTime d)
    {
        var t = d.ToString("dddd d. MMMM", Kultur);
        return char.ToUpper(t[0]) + t[1..];
    }

    public static string? Adresse(Arbeidsordre a)
    {
        var deler = new[] { a.Adresse ?? a.Prosjekt?.Adresse, a.Sted ?? a.Prosjekt?.Sted }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        return deler.Count > 0 ? string.Join(", ", deler) : null;
    }

    public static string? Adresse(Prosjekt p)
    {
        var deler = new[] { p.Adresse, p.Sted }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        return deler.Count > 0 ? string.Join(", ", deler) : null;
    }

    public static string KartLenke(string adresse) => "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(adresse);

    public static string StatusKlasse(ArbeidsordreStatus s) => s switch
    {
        ArbeidsordreStatus.Ny => "ny",
        ArbeidsordreStatus.Klar => "klar",
        ArbeidsordreStatus.Pagaende => "pagaende",
        _ => "ferdig"
    };

    public static string Initialer(string navn) =>
        string.Concat(navn.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s => char.ToUpperInvariant(s[0])));
}
