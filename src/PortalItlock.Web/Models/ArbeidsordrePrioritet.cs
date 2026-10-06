namespace PortalItlock.Web.Models;

public enum ArbeidsordrePrioritet { Lav, Normal, Hoy, Kritisk }

public static class ArbeidsordrePrioritetExtensions
{
    public static string Visningsnavn(this ArbeidsordrePrioritet prioritet) => prioritet switch
    {
        ArbeidsordrePrioritet.Lav => "Lav",
        ArbeidsordrePrioritet.Normal => "Normal",
        ArbeidsordrePrioritet.Hoy => "Høy",
        ArbeidsordrePrioritet.Kritisk => "Kritisk",
        _ => prioritet.ToString()
    };

    public static string PillIkon(this ArbeidsordrePrioritet prioritet) => prioritet switch
    {
        ArbeidsordrePrioritet.Kritisk => "alert",
        _ => "tag"
    };

    public static string PillFarge(this ArbeidsordrePrioritet prioritet) => prioritet switch
    {
        ArbeidsordrePrioritet.Lav => "gronn",
        ArbeidsordrePrioritet.Normal => "noytral",
        ArbeidsordrePrioritet.Hoy => "gul",
        ArbeidsordrePrioritet.Kritisk => "rod",
        _ => "noytral"
    };
}
