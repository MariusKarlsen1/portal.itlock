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

    public static string PillIkon(this MontasjeStatus status) => status switch
    {
        MontasjeStatus.IkkeStartet => "clock",
        MontasjeStatus.Montert => "tool",
        MontasjeStatus.FerdigMontert => "check-circle",
        _ => "clock"
    };

    public static string PillFarge(this MontasjeStatus status) => status switch
    {
        MontasjeStatus.IkkeStartet => "blaa",
        MontasjeStatus.Montert => "gul",
        MontasjeStatus.FerdigMontert => "gronn",
        _ => "noytral"
    };
}
