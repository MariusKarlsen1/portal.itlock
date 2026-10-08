using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public sealed record GrunndataRad(string Ikon, string Label, string Verdi);

// "Grunndata"-visningen (brukt av både Dørpakker sin splitt-visning og
// dørpakke-detaljsiden) er en ren fremstilling av pakkens EKSISTERENDE krav
// (RequirementDimension/-Value, samme data som kravvelgeren på
// /finn-dorpakke og /rediger-pakke bruker) - ingen egne felter, bare en
// annen fremstilling: én rad per dimensjon i stedet for avkrysningsbokser.
// "FG" vises som "FG-godkjenning" og dimensjonen "Lukkefunksjon" vises som
// et utledet "Automatikk: Ja/Nei" (Ja når valgt verdi inneholder
// "Automatikk"), for å matche det oppgitte designet.
public static class PakkeVisningHjelper
{
    public static List<string> TopPiller(Package p, int antall = 3) =>
        p.Krav
            .Where(k => k.RequirementValue is not null)
            .OrderBy(k => k.RequirementValue!.Dimensjon?.Rekkefolge ?? 99)
            .Select(k => k.RequirementValue!.Verdi)
            .Take(antall)
            .ToList();

    public static List<GrunndataRad> Grunndata(Package p)
    {
        string? VerdiFor(int dimensjonId) =>
            p.Krav.FirstOrDefault(k => k.RequirementValue?.RequirementDimensionId == dimensjonId)?.RequirementValue?.Verdi;

        var rader = new List<GrunndataRad>();

        if (VerdiFor(1) is { } typeDor) { rader.Add(new("box", "Type dør", typeDor)); }
        if (VerdiFor(2) is { } hvilkeBruk) { rader.Add(new("lock", "Hvilke bruk", hvilkeBruk)); }
        if (VerdiFor(3) is { } fg) { rader.Add(new("shield", "FG-godkjenning", fg)); }
        if (VerdiFor(4) is { } risikoklasse) { rader.Add(new("alert", "Risikoklasse", risikoklasse)); }

        var lukkefunksjon = VerdiFor(5);
        if (lukkefunksjon is not null)
        {
            var erAutomatikk = lukkefunksjon.Contains("Automatikk", StringComparison.OrdinalIgnoreCase);
            rader.Add(new("toggle-right", "Automatikk", erAutomatikk ? "Ja" : "Nei"));
        }

        if (VerdiFor(6) is { } antallFloyer) { rader.Add(new("maximize", "Antall fløyer", antallFloyer)); }
        if (VerdiFor(7) is { } typeBeslag) { rader.Add(new("tool", "Type beslag", typeBeslag)); }
        if (VerdiFor(8) is { } tilbakeromning) { rader.Add(new("arrow-left", "Tilbakerømning", tilbakeromning)); }
        if (VerdiFor(9) is { } postsonesylinder) { rader.Add(new("mail", "Postsonesylinder", postsonesylinder)); }

        return rader;
    }
}
