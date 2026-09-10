namespace PortalItlock.Web.Models;

public enum ProsjektStatus
{
    Aktiv,
    Tilbud,
    TilbudAvslatt,
    Registrert,
    Avsluttet,
    Serviceavtale,
    Overlevert
}

public static class ProsjektStatusExtensions
{
    public static string Visningsnavn(this ProsjektStatus status) => status switch
    {
        ProsjektStatus.Aktiv => "Aktiv (prosjektering pågår)",
        ProsjektStatus.Tilbud => "Tilbud",
        ProsjektStatus.TilbudAvslatt => "Tilbud avslått",
        ProsjektStatus.Registrert => "Registrert",
        ProsjektStatus.Avsluttet => "Avsluttet",
        ProsjektStatus.Serviceavtale => "Serviceavtale",
        ProsjektStatus.Overlevert => "Overlevert",
        _ => status.ToString()
    };
}
