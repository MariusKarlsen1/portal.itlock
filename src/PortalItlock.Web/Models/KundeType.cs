namespace PortalItlock.Web.Models;

public enum KundeType { Privatperson, Bedriftskunde, Entreprenor, Borettslag, Sameie, Eiendomsforvalter, DetOffentlige, Annet }

public static class KundeTypeExtensions
{
    public static string Visningsnavn(this KundeType type) => type switch
    {
        KundeType.Privatperson => "Privatperson",
        KundeType.Bedriftskunde => "Kunde bedrift",
        KundeType.Entreprenor => "Entreprenør",
        KundeType.Borettslag => "Borettslag",
        KundeType.Sameie => "Sameie",
        KundeType.Eiendomsforvalter => "Eiendomsforvalter",
        KundeType.DetOffentlige => "Det offentlige",
        KundeType.Annet => "Annet",
        _ => type.ToString()
    };
}
