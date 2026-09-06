namespace PortalItlock.Web.Models;

public enum TicketStatus
{
    Ny,
    UnderBehandling,
    VenterGodkjenning,
    Lukket
}

public static class TicketStatusExtensions
{
    public static readonly TicketStatus[] Rekkefolge =
    [
        TicketStatus.Ny,
        TicketStatus.UnderBehandling,
        TicketStatus.VenterGodkjenning,
        TicketStatus.Lukket
    ];

    public static string Visningsnavn(this TicketStatus status) => status switch
    {
        TicketStatus.Ny => "Ny",
        TicketStatus.UnderBehandling => "Under behandling",
        TicketStatus.VenterGodkjenning => "Venter godkjenning",
        TicketStatus.Lukket => "Lukket",
        _ => status.ToString()
    };
}
