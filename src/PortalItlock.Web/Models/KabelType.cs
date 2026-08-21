namespace PortalItlock.Web.Models;

public enum KabelType
{
    CatKabel,
    TiPar,
    Atteleder,
    Strom230V,
    Patchekabel
}

public static class KabelTypeExtensions
{
    public static string Visningsnavn(this KabelType type) => type switch
    {
        KabelType.CatKabel => "CAT-kabel",
        KabelType.TiPar => "10-par",
        KabelType.Atteleder => "8-leder",
        KabelType.Strom230V => "230V strøm",
        KabelType.Patchekabel => "Patchekabel",
        _ => type.ToString()
    };

    public static string Farge(this KabelType type) => type switch
    {
        KabelType.CatKabel => "#2b7a4b",
        KabelType.TiPar => "#8b5cf6",
        KabelType.Atteleder => "#0f9b8e",
        KabelType.Strom230V => "#dc2626",
        KabelType.Patchekabel => "#0369a1",
        _ => "#333333"
    };

    /// <summary>SVG stroke-dasharray - gir hver kabeltype et eget linjemønster i tillegg til farge.</summary>
    public static string StrekMonster(this KabelType type) => type switch
    {
        KabelType.CatKabel => "",
        KabelType.TiPar => "3,1.5",
        KabelType.Atteleder => "1.5,1",
        KabelType.Strom230V => "4,1,0.5,1",
        KabelType.Patchekabel => "2,2",
        _ => ""
    };
}
