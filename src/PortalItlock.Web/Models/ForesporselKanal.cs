namespace PortalItlock.Web.Models;

public enum ForesporselKanal
{
    Epost,
    Telefon,
    Skjema
}

public static class ForesporselKanalExtensions
{
    public static string Visningsnavn(this ForesporselKanal kanal) => kanal switch
    {
        ForesporselKanal.Epost => "E-post",
        ForesporselKanal.Telefon => "Telefon",
        ForesporselKanal.Skjema => "Skjema",
        _ => kanal.ToString()
    };

    public static string Ikon(this ForesporselKanal kanal) => kanal switch
    {
        ForesporselKanal.Epost => "mail",
        ForesporselKanal.Telefon => "phone",
        ForesporselKanal.Skjema => "file-text",
        _ => "mail"
    };
}
