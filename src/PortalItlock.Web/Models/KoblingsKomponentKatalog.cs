namespace PortalItlock.Web.Models;

// Én innebygd komponenttype i komponentpaletten til koblingsskjema-editoren.
// Nokkel lagres på KoblingsSymbol.KomponentNokkel, og Ikon peker på et ikon i
// det delte Icon-biblioteket (alle har prefiks "komp-").
public sealed record KoblingsKomponent(string Nokkel, string Navn, string Ikon);

// Én kabeltype i "Kabling"-tegnforklaringen nederst i paletten. Farge og
// Stiplet speiler standardverdiene streken får når montøren velger typen,
// slik at forklaringen alltid viser det som faktisk tegnes.
public sealed record KoblingsKabelType(string Navn, string Farge, bool Stiplet);

// Fast katalog over komponentene montørene bygger koblingsskjema av. Ligger i
// kode (ikke database) fordi det er en felles, bransjebestemt liste som skal
// være lik for alle organisasjoner - i motsetning til KoblingsSymbolBibliotek,
// som er bilder den enkelte organisasjonen selv laster opp.
public static class KoblingsKomponentKatalog
{
    public static readonly List<KoblingsKomponent> Elektronikk =
    [
        new("adk-sentral", "ADK-sentral", "komp-adk-sentral"),
        new("dorkort", "Dørkort", "komp-dorkort"),
        new("psu", "PSU", "komp-psu"),
        new("switch", "Switch", "komp-switch"),
        new("kac", "KAC", "komp-kac"),
        new("rele", "Relé", "komp-rele"),
        new("kortleser-inn", "Kortleser inn", "komp-kortleser-inn"),
        new("kortleser-ut", "Kortleser ut", "komp-kortleser-ut"),
        new("apneknapp", "Åpneknapp", "komp-apneknapp"),
        new("albuebryter", "Albuebryter", "komp-albuebryter"),
        new("bevegelsessensor", "Bevegelsessensor", "komp-bevegelsessensor"),
        new("flatscan", "Flatscan", "komp-flatscan"),
        new("dorpumpe", "Dørpumpe", "komp-dorpumpe"),
        new("dorautomatikk", "Dørautomatikk", "komp-dorautomatikk"),
        new("magnetkontakt", "Magnetkontakt", "komp-magnetkontakt"),
        new("el-sluttstykke", "El. sluttstykke", "komp-el-sluttstykke"),
        new("dorkontakt", "Dørkontakt", "komp-dorkontakt"),
    ];

    public static readonly List<KoblingsKabelType> Kabling =
    [
        new("Nettverk (Cat6)", "#2f6f8f", false),
        new("Strøm 230V", "#b23b3b", false),
        new("Strøm 12/24V", "#c98a2e", false),
        new("Signal/data", "#835e41", false),
        new("Buss (OSDP/RS485)", "#5d7a4a", true),
        new("Trekkerør/reserve", "#6b6863", true),
    ];

    public static KoblingsKomponent? Finn(string? nokkel) =>
        nokkel is null ? null : Elektronikk.FirstOrDefault(k => k.Nokkel == nokkel);
}
