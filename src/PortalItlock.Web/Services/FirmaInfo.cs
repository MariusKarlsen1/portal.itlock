namespace PortalItlock.Web.Services;

// Felles firmainfo brukt i topp-/bunntekst på alle genererte PDF-er.
// Endres ett sted her i stedet for i hver enkelt *PdfService.
public static class FirmaInfo
{
    public const string Navn = "ITLOCK AS";
    public const string Adresse = "Gartnerveien 2";
    public const string Postnr = "4374";
    public const string Sted = "Egersund";
    public const string AdresseFull = "Gartnerveien 2, 4374 Egersund";
    public const string Telefon = "47355441";
    public const string Epost = "marius@itlock.no";
    public const string Kontaktperson = "Marius Karlsen";
}
