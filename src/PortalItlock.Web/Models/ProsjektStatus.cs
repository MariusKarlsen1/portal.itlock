namespace PortalItlock.Web.Models;

public enum ProsjektStatus
{
    Aktiv,
    Tilbud,
    TilbudAvslatt,
    Registrert,
    Avsluttet,
    Serviceavtale,
    Overlevert,

    // Lagt til etter de andre (ikke midt i lista) for at de lagrede
    // tallverdiene til eksisterende prosjekter ikke skal endre betydning.
    UnderArbeid
}

public static class ProsjektStatusExtensions
{
    public static string Visningsnavn(this ProsjektStatus status) => status switch
    {
        ProsjektStatus.Aktiv => "Aktiv (prosjektering pågår)",
        ProsjektStatus.Tilbud => "Tilbud sendt",
        ProsjektStatus.TilbudAvslatt => "Tilbud avslått",
        ProsjektStatus.Registrert => "Registrert",
        ProsjektStatus.Avsluttet => "Avsluttet",
        ProsjektStatus.Serviceavtale => "Serviceavtale",
        ProsjektStatus.Overlevert => "Overlevert",
        ProsjektStatus.UnderArbeid => "Under arbeid",
        _ => status.ToString()
    };

    public static string PillIkon(this ProsjektStatus status) => status switch
    {
        ProsjektStatus.Aktiv => "tool",
        ProsjektStatus.Tilbud => "receipt",
        ProsjektStatus.TilbudAvslatt => "x",
        ProsjektStatus.Registrert => "folder",
        ProsjektStatus.Avsluttet => "check-circle",
        ProsjektStatus.Serviceavtale => "shield",
        ProsjektStatus.Overlevert => "check-circle",
        ProsjektStatus.UnderArbeid => "clock",
        _ => "folder"
    };

    public static string PillFarge(this ProsjektStatus status) => status switch
    {
        ProsjektStatus.Aktiv => "blaa",
        ProsjektStatus.Tilbud => "gul",
        ProsjektStatus.TilbudAvslatt => "rod",
        ProsjektStatus.Registrert => "blaa",
        ProsjektStatus.Avsluttet => "noytral",
        ProsjektStatus.Serviceavtale => "lilla",
        ProsjektStatus.Overlevert => "gronn",
        ProsjektStatus.UnderArbeid => "blaa",
        _ => "noytral"
    };
}
