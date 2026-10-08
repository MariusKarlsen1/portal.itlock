namespace PortalItlock.Web.Models;

// Ferdigdefinerte fargetemaer en organisasjon kan velge mellom på
// /plattform (PlattformOrganisasjonDetalj.razor) - erstatter de tidligere
// frie fargevelgerne (Tenant.TemaFarge/TemaBakgrunn) med et begrenset,
// kuratert utvalg, slik at enhver organisasjon alltid får et
// gjennomtenkt og lesbart fargesett i stedet for en vilkårlig hex-kode.
public sealed record OrganisasjonTema(
    string Id,
    string Navn,
    string Beskrivelse,
    string Primaer,
    string PrimaerHover,
    string Sekundaer,
    string SekundaerLys,
    string PrimaerLys,
    string Bakgrunn)
{
    public static readonly OrganisasjonTema Standard = new(
        Id: "Standard",
        Navn: "Varmbrun",
        Beskrivelse: "Dagens farger hos itlock - varm og personlig, med brun som hovedfarge.",
        Primaer: "#835E41",
        PrimaerHover: "#6E4D36",
        Sekundaer: "#6B6354",
        SekundaerLys: "#D8CBB4",
        PrimaerLys: "#F1EADD",
        Bakgrunn: "#F4F0E9");

    public static readonly OrganisasjonTema Marinbla = new(
        Id: "Marinbla",
        Navn: "Marineblå",
        Beskrivelse: "Profesjonell, moderne og tillitsfull. Samme varme bakgrunn som i dag, med marinblå som hovedfarge.",
        Primaer: "#0A2540",
        PrimaerHover: "#143A50",
        Sekundaer: "#1E4E79",
        SekundaerLys: "#4E7CA7",
        PrimaerLys: "#EBF1FA",
        Bakgrunn: "#F7F4EE");

    public static readonly IReadOnlyList<OrganisasjonTema> Alle = [Standard, Marinbla];

    public static OrganisasjonTema FraId(string? id) =>
        Alle.FirstOrDefault(t => t.Id == id) ?? Standard;
}
