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
    string Bakgrunn,
    // Mørk sidemeny ("mork") eller den vanlige hvite (null), ren skrift
    // (Inter Tight) i stedet for Fraunces, og kantfarge for kjølige temaer.
    string? Meny = null,
    bool RenSkrift = false,
    string? Kant = null)
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

    // Marineblå (oktober 2026): mørk marineblå sidemeny, kjølig lys
    // bakgrunn, blå knapper og ren skrift - profesjonelt og ryddig.
    public static readonly OrganisasjonTema Marinbla = new(
        Id: "Marinbla",
        Navn: "Marineblå",
        Beskrivelse: "Profesjonell og ryddig: mørk marineblå meny, lys kjølig bakgrunn, blå knapper og ren skrift.",
        Primaer: "#1F4E8C",
        PrimaerHover: "#173D70",
        Sekundaer: "#14213D",
        SekundaerLys: "#5B6680",
        PrimaerLys: "#E7EEF8",
        Bakgrunn: "#EEF1F6",
        Meny: "mork",
        RenSkrift: true,
        Kant: "#DDE3EC");

    public static readonly IReadOnlyList<OrganisasjonTema> Alle = [Standard, Marinbla];

    public static OrganisasjonTema FraId(string? id) =>
        Alle.FirstOrDefault(t => t.Id == id) ?? Standard;
}
