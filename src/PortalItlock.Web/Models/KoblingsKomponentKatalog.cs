namespace PortalItlock.Web.Models;

// Én innebygd komponenttype i komponentpaletten til koblingsskjema-editoren.
// Nokkel lagres på KoblingsSymbol.KomponentNokkel, og Ikon peker på et ikon i
// det delte Icon-biblioteket (alle har prefiks "komp-").
//
// Bilde peker på en produktlik SVG-tegning i wwwroot/img/kobling. Bredde er
// startbredden i prosent av lerretet og Forhold er høyde/bredde på tegningen,
// slik at den legges inn uten å bli strukket.
public sealed record KoblingsKomponent(string Nokkel, string Navn, string Ikon, string? Bilde = null, double Bredde = 8, double Forhold = 1);

public sealed record KoblingsKomponentGruppe(string Tittel, List<KoblingsKomponent> Komponenter);

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

    // Egne, produktlike tegninger av sentraler og dørmoduler med klemmenavn.
    public static readonly List<KoblingsKomponent> Sentraler =
    [
        Tegning("salto-cu4200", "SALTO CU4200", 22, 300, 240),
        Tegning("arx-dac500", "ARX DAC 500", 12, 160, 280),
        Tegning("arx-dac630", "ARX DAC630", 13, 180, 300),
        Tegning("iloq-n50x", "iLOQ N50x dørmodul", 17, 240, 200),
        Tegning("paxton-net2plus", "Paxton Net2 plus", 20, 280, 220),
        Tegning("paxton10-sdc", "Paxton10 dørkontroller", 17, 240, 180),
        Tegning("rco-rcard-m5", "RCO R-CARD M5", 19, 260, 190),
        Tegning("koblingsboks", "Koblingsboks 40", 14, 200, 260),
        Tegning("stromforsyning", "Strømforsyning", 9, 120, 100),
        Tegning("rele", "Relé", 9, 120, 80),
    ];

    // Utstyr ved døra, tegnet slik det ser ut.
    public static readonly List<KoblingsKomponent> VedDora =
    [
        Tegning("kortleser-tastatur", "Kortleser m/tastatur", 6, 90, 150),
        Tegning("kortleser", "Kortleser", 5, 70, 120),
        Tegning("nokkelbryter", "Nøkkelbryter", 6, 90, 90),
        Tegning("albuebryter", "Albuebryter", 5.5, 80, 110),
        Tegning("nodapner", "Nødåpner (grønn)", 7, 110, 110),
        Tegning("magnetkontakt", "Magnetkontakt", 4.5, 70, 108),
        Tegning("el-sluttstykke", "El-sluttstykke", 5, 80, 140),
        Tegning("motorlas", "Motorlås", 4.5, 60, 180),
        Tegning("magnetlas", "Magnetlås", 13, 184, 70),
        Tegning("holdemagnet", "Holdemagnet", 7, 96, 78),
        Tegning("dorlukker", "Dørlukker", 11, 150, 80),
        Tegning("dorautomatikk", "Dørautomatikk", 14, 194, 82),
    ];

    // Komponentene paletten viser: illustrasjoner (PNG med gjennomsiktig
    // bakgrunn) i wwwroot/img/kobling/egne.
    public static readonly List<KoblingsKomponent> LaserOgSluttstykker =
    [
        Illustrasjon("sluttstykke-kompakt", "El-sluttstykke kompakt", 4.5, 103, 232),
        Illustrasjon("sluttstykke", "El-sluttstykke", 5, 107, 289),
        Illustrasjon("sluttstykke-dobbel", "El-sluttstykke dobbel", 4.5, 98, 330),
        Illustrasjon("sluttstykke-kraftig", "El-sluttstykke kraftig", 5.5, 135, 372),
        Illustrasjon("motorlas-kabel-hvit", "Motorlås med kabel", 8, 198, 338),
        Illustrasjon("motorlas-kabel-sort", "Motorlås med kabel, sort", 8, 209, 337),
        Illustrasjon("krokfallas-vrider", "Krokfallås (vrider)", 7, 159, 335),
        Illustrasjon("krokfallas-sylinder", "Krokfallås (sylinder)", 7.5, 178, 340),
    ];

    public static readonly List<KoblingsKomponent> LesereOgSentraler =
    [
        Illustrasjon("kodeleser-stal", "Kodeleser (stål)", 6, 269, 638),
        Illustrasjon("kodeleser-sort", "Kodeleser (sort)", 6, 270, 639),
        Illustrasjon("dorsentral-boks", "Dørsentral i boks", 22, 758, 676),
        Illustrasjon("salto-cu42e0", "SALTO CU42E0", 20, 371, 436),
        Illustrasjon("salto-kodeleser", "SALTO kodeleser", 7, 293, 316),
        Illustrasjon("iloq-kodeleser", "iLOQ kodeleser", 6, 377, 677),
        Illustrasjon("iloq-leser-slank", "iLOQ leser (slank)", 4, 130, 298),
    ];

    public static readonly List<KoblingsKomponent> UtstyrVedDora =
    [
        Illustrasjon("apneknapp", "Åpneknapp", 4.5, 142, 315),
        Illustrasjon("beroringsfri", "Berøringsfri åpneknapp", 3, 93, 321),
        Illustrasjon("radar", "Radar / bevegelsessensor", 9, 279, 166),
        Illustrasjon("nodapner", "Nødåpner", 7, 237, 260),
        Illustrasjon("magnetkontakt", "Magnetkontakt", 10, 473, 110),
        Illustrasjon("dorautomatikk", "Dørautomatikk", 14, 429, 194),
        Illustrasjon("exit-knapp", "Exit-knapp", 6, 185, 190),
        Illustrasjon("nokkelbryter-vippe", "Nøkkelbryter (vippe)", 6, 178, 197),
        Illustrasjon("nokkelbryter-sylinder", "Nøkkelbryter (sylinder)", 6, 198, 202),
        Illustrasjon("magnetlas", "Magnetlås", 13, 384, 227),
    ];

    public static readonly List<KoblingsKomponentGruppe> Grupper =
    [
        new("Låser og sluttstykker", LaserOgSluttstykker),
        new("Lesere og sentraler", LesereOgSentraler),
        new("Ved døra", UtstyrVedDora),
    ];

    // Eldre komponenter vises ikke lenger i paletten, men koblingsskjemaer som
    // allerede bruker dem skal fortsatt tegnes riktig.
    private static readonly List<KoblingsKomponent> Utgatte = [.. Sentraler, .. VedDora, .. Elektronikk];

    private static KoblingsKomponent Illustrasjon(string nokkel, string navn, double bredde, double w, double h) =>
        new($"b-{nokkel}", navn, "box", $"/img/kobling/egne/{nokkel}.png", bredde, h / w);

    private static KoblingsKomponent Tegning(string nokkel, string navn, double bredde, double w, double h) =>
        new($"p-{nokkel}", navn, "box", $"/img/kobling/{nokkel}.svg", bredde, h / w);

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
        nokkel is null ? null : Grupper.SelectMany(g => g.Komponenter).Concat(Utgatte).FirstOrDefault(k => k.Nokkel == nokkel);
}
