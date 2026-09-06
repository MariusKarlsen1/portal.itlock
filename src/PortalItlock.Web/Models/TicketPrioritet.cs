namespace PortalItlock.Web.Models;

public enum TicketPrioritet
{
    Lav,
    Normal,
    Hoy,
    Kritisk
}

public static class TicketPrioritetExtensions
{
    public static string Visningsnavn(this TicketPrioritet prioritet) => prioritet switch
    {
        TicketPrioritet.Lav => "Lav",
        TicketPrioritet.Normal => "Normal",
        TicketPrioritet.Hoy => "Høy",
        TicketPrioritet.Kritisk => "Kritisk",
        _ => prioritet.ToString()
    };
}
