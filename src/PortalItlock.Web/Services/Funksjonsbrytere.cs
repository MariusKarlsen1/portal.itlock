namespace PortalItlock.Web.Services;

// Funksjoner som er slått av i koden uten at noe er slettet. Sett til true for å
// få dem tilbake - sider, data og tabeller ligger urørt.
public static class Funksjonsbrytere
{
    // Tickets (november 2026): ikke i bruk lenger. Skjult fra meny, varsler,
    // kundekortet, søk og dashbord. /tickets sender til forsiden.
    public static bool Tickets { get; } = false;

    public static readonly string[] TicketLenker = ["tickets", "ticket-kategorier"];
}
