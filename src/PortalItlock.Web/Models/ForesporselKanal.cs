namespace PortalItlock.Web.Models;

public enum ForesporselKanal
{
    Epost,
    Telefon,
    Skjema,

    // Driftsmelding fra vaktmester i driftsportalen (se DriftsmeldingForesporsel).
    Driftsportal
}

public static class ForesporselKanalExtensions
{
    public static string Visningsnavn(this ForesporselKanal kanal) => kanal switch
    {
        ForesporselKanal.Epost => "E-post",
        ForesporselKanal.Telefon => "Telefon",
        ForesporselKanal.Skjema => "Skjema",
        ForesporselKanal.Driftsportal => "Driftsmelding",
        _ => kanal.ToString()
    };

    public static string Ikon(this ForesporselKanal kanal) => kanal switch
    {
        ForesporselKanal.Epost => "mail",
        ForesporselKanal.Telefon => "phone",
        ForesporselKanal.Skjema => "file-text",
        ForesporselKanal.Driftsportal => "door",
        _ => "mail"
    };
}
