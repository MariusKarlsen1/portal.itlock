using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Felles logikk for kundeportalen (/kunde): hvilke jobber som hører til
// kunden, og hvordan status på henvendelser og saker vises.
public static class KundeportalHelper
{
    // Arbeidsordre kunden skal se: på kundens prosjekter, eller satt direkte på kunden.
    public static IQueryable<Arbeidsordre> Jobber(ApplicationDbContext db, int kundeId) =>
        db.Arbeidsordre.Where(a => (a.Prosjekt != null && a.Prosjekt.KundeId == kundeId) || a.KundeId == kundeId);

    public static bool HorerTilKunde(Arbeidsordre a, int kundeId) =>
        a.Prosjekt?.KundeId == kundeId || a.KundeId == kundeId;

    // Som før: kunden ser jobben som ferdig (med rapport) først når den er
    // fakturert - da er rapporten også sendt til dem.
    public static bool ErFerdig(Arbeidsordre a) => a.Status == ArbeidsordreStatus.Fakturert;

    public static string Farge(ServicehenvendelseStatus s) => s switch
    {
        ServicehenvendelseStatus.Ny => "gra",
        ServicehenvendelseStatus.UnderVurdering => "bla",
        ServicehenvendelseStatus.PrisGitt or ServicehenvendelseStatus.VarerBestilt => "gul",
        ServicehenvendelseStatus.Fullfort => "gronn",
        _ => "rod"
    };

    public static string Ikon(ServicehenvendelseStatus s) => s switch
    {
        ServicehenvendelseStatus.Fullfort => "check",
        ServicehenvendelseStatus.Avslatt => "x",
        ServicehenvendelseStatus.Ny => "send",
        _ => "clock"
    };

    // Kundevennlig navn - "Ny" betyr for kunden at vi har mottatt den.
    public static string Navn(ServicehenvendelseStatus s) => s == ServicehenvendelseStatus.Ny ? "Mottatt" : s.Visningsnavn();

    public static string Farge(TicketStatus s) => s switch
    {
        TicketStatus.Ny => "bla",
        TicketStatus.UnderBehandling => "gul",
        _ => "gronn"
    };

    public static string Navn(TicketStatus s) => s switch
    {
        TicketStatus.Ny => "Mottatt",
        TicketStatus.UnderBehandling => "Under behandling",
        _ => "Løst"
    };

    public static string Ukedag(DateTime d) => d.ToString("ddd", new System.Globalization.CultureInfo("nb-NO")).TrimEnd('.');

    public static string Initialer(string navn) =>
        string.Concat(navn.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(d => char.ToUpperInvariant(d[0])));
}
