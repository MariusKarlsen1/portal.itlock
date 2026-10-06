namespace PortalItlock.Web.Models;

public enum ToDoPrioritet
{
    Lav,
    Normal,
    Hoy
}

public static class ToDoPrioritetExtensions
{
    public static string Visningsnavn(this ToDoPrioritet prioritet) => prioritet switch
    {
        ToDoPrioritet.Lav => "Lav",
        ToDoPrioritet.Normal => "Normal",
        ToDoPrioritet.Hoy => "Høy",
        _ => prioritet.ToString()
    };

    public static string Farge(this ToDoPrioritet prioritet) => prioritet switch
    {
        ToDoPrioritet.Lav => "gronn",
        ToDoPrioritet.Normal => "gul",
        ToDoPrioritet.Hoy => "rod",
        _ => "noytral"
    };
}
