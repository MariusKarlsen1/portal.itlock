namespace PortalItlock.Web.Models;

public enum FravarType
{
    Ferie,
    Egenmelding,
    Sykemelding,
    Permisjon,
    Annet,
    SyktBarn
}

public static class FravarTypeExtensions
{
    public static string Visningsnavn(this FravarType type) => type switch
    {
        FravarType.Ferie => "Ferie",
        FravarType.Egenmelding => "Egenmelding",
        FravarType.Sykemelding => "Sykemelding",
        FravarType.Permisjon => "Permisjon",
        FravarType.Annet => "Annet",
        FravarType.SyktBarn => "Sykt barn",
        _ => type.ToString()
    };
}
