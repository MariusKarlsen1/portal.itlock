namespace PortalItlock.Web.Models;

public enum PlanUtstyrType
{
    Kortleser,
    Kac,
    Apneknapp,
    Sentral,
    Stikkontakt,
    Ups
}

public static class PlanUtstyrTypeExtensions
{
    public static string Visningsnavn(this PlanUtstyrType type) => type switch
    {
        PlanUtstyrType.Kortleser => "Kortleser",
        PlanUtstyrType.Kac => "KAC",
        PlanUtstyrType.Apneknapp => "Åpneknapp",
        PlanUtstyrType.Sentral => "Sentral",
        PlanUtstyrType.Stikkontakt => "Stikkontakt",
        PlanUtstyrType.Ups => "UPS",
        _ => type.ToString()
    };

    public static string Kode(this PlanUtstyrType type) => type switch
    {
        PlanUtstyrType.Kortleser => "KL",
        PlanUtstyrType.Kac => "KAC",
        PlanUtstyrType.Apneknapp => "ÅK",
        PlanUtstyrType.Sentral => "SE",
        PlanUtstyrType.Stikkontakt => "SK",
        PlanUtstyrType.Ups => "UPS",
        _ => "?"
    };

    public static string Farge(this PlanUtstyrType type) => type switch
    {
        PlanUtstyrType.Kortleser => "#2f6fb3",
        PlanUtstyrType.Kac => "#8b5cf6",
        PlanUtstyrType.Apneknapp => "#2b7a4b",
        PlanUtstyrType.Sentral => "#d9822b",
        PlanUtstyrType.Stikkontakt => "#6b7280",
        PlanUtstyrType.Ups => "#b5502d",
        _ => "#333333"
    };
}
