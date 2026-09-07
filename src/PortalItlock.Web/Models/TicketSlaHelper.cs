namespace PortalItlock.Web.Models;

public static class TicketSlaHelper
{
    public static DateTime BeregnFrist(TicketPrioritet prioritet, DateTime fra) => prioritet switch
    {
        TicketPrioritet.Kritisk => fra.AddHours(4),
        TicketPrioritet.Hoy => fra.AddDays(1),
        TicketPrioritet.Normal => fra.AddDays(3),
        TicketPrioritet.Lav => fra.AddDays(7),
        _ => fra.AddDays(3)
    };
}
