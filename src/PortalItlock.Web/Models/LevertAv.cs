namespace PortalItlock.Web.Models;

/// <summary>
/// Hvem som leverer/forbereder et beslag: F = itlock leverer og priser det selv,
/// Ld = dørprodusenten leverer det komplett, Le = elektro leverer det komplett.
/// </summary>
public enum LevertAv
{
    F,
    Ld,
    Le
}

public static class LevertAvExtensions
{
    public static string Visningsnavn(this LevertAv levertAv) => levertAv switch
    {
        LevertAv.F => "F",
        LevertAv.Ld => "Ld",
        LevertAv.Le => "Le",
        _ => levertAv.ToString()
    };

    public static string Forklaring(this LevertAv levertAv) => levertAv switch
    {
        LevertAv.F => "Ferdig forberedt – itlock leverer og priser",
        LevertAv.Ld => "Leveres komplett av dørprodusent",
        LevertAv.Le => "Leveres komplett av elektro",
        _ => ""
    };
}
