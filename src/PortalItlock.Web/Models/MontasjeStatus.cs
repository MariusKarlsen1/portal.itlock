namespace PortalItlock.Web.Models;

public enum MontasjeStatus
{
    IkkeStartet,
    Montert,
    FerdigMontert
}

public static class MontasjeStatusExtensions
{
    public static string Visningsnavn(this MontasjeStatus status) => status switch
    {
        MontasjeStatus.IkkeStartet => "Ikke startet",
        MontasjeStatus.Montert => "Montasje pågår",
        MontasjeStatus.FerdigMontert => "Ferdig montert",
        _ => status.ToString()
    };
}
