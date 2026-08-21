namespace PortalItlock.Web.Models;

public enum PlanUtstyrType
{
    Kortleser,
    Kac,
    Apneknapp,
    Sentral,
    Stikkontakt,
    Ups,
    Datapunkt,
    Pir,
    Alarmpanel,
    Hovedsentral,
    Kamera,
    Dorautomatikk,
    Sirene,
    Albuebryter,
    Nokkelbryter,
    Grensesnittboks,
    Iq,
    Node,
    Gateway,
    Hub
}

public static class PlanUtstyrTypeExtensions
{
    public static string Visningsnavn(this PlanUtstyrType type) => type switch
    {
        PlanUtstyrType.Kortleser => "Kortleser",
        PlanUtstyrType.Kac => "KAC nødåpner",
        PlanUtstyrType.Apneknapp => "Åpneknapp",
        PlanUtstyrType.Sentral => "Sentral",
        PlanUtstyrType.Stikkontakt => "Stikkontakt",
        PlanUtstyrType.Ups => "UPS",
        PlanUtstyrType.Datapunkt => "Datapunkt",
        PlanUtstyrType.Pir => "PIR-detektor",
        PlanUtstyrType.Alarmpanel => "Alarmpanel",
        PlanUtstyrType.Hovedsentral => "Hovedsentral",
        PlanUtstyrType.Kamera => "Kamera",
        PlanUtstyrType.Dorautomatikk => "Dørautomatikk",
        PlanUtstyrType.Sirene => "Sirene",
        PlanUtstyrType.Albuebryter => "Albuebryter",
        PlanUtstyrType.Nokkelbryter => "Nøkkelbryter",
        PlanUtstyrType.Grensesnittboks => "Grensesnittboks",
        PlanUtstyrType.Iq => "IQ-lås",
        PlanUtstyrType.Node => "Trådløs node",
        PlanUtstyrType.Gateway => "Gateway",
        PlanUtstyrType.Hub => "Hub",
        _ => type.ToString()
    };

    public static string Kode(this PlanUtstyrType type) => type switch
    {
        PlanUtstyrType.Kortleser => "KL",
        PlanUtstyrType.Kac => "KAC",
        PlanUtstyrType.Apneknapp => "ÅK",
        PlanUtstyrType.Sentral => "SE",
        PlanUtstyrType.Stikkontakt => "SK",
        PlanUtstyrType.Ups => "UPS",
        PlanUtstyrType.Datapunkt => "DP",
        PlanUtstyrType.Pir => "PIR",
        PlanUtstyrType.Alarmpanel => "AP",
        PlanUtstyrType.Hovedsentral => "HS",
        PlanUtstyrType.Kamera => "CAM",
        PlanUtstyrType.Dorautomatikk => "DA",
        PlanUtstyrType.Sirene => "SIR",
        PlanUtstyrType.Albuebryter => "AB",
        PlanUtstyrType.Nokkelbryter => "NB",
        PlanUtstyrType.Grensesnittboks => "GB",
        PlanUtstyrType.Iq => "IQ",
        PlanUtstyrType.Node => "NOD",
        PlanUtstyrType.Gateway => "GW",
        PlanUtstyrType.Hub => "HUB",
        _ => "?"
    };

    public static string Farge(this PlanUtstyrType type) => type switch
    {
        PlanUtstyrType.Kortleser => "#2f6fb3",
        PlanUtstyrType.Kac => "#16a34a",
        PlanUtstyrType.Apneknapp => "#2b7a4b",
        PlanUtstyrType.Sentral => "#d9822b",
        PlanUtstyrType.Stikkontakt => "#6b7280",
        PlanUtstyrType.Ups => "#b5502d",
        PlanUtstyrType.Datapunkt => "#0f9b8e",
        PlanUtstyrType.Pir => "#c2410c",
        PlanUtstyrType.Alarmpanel => "#7c3aed",
        PlanUtstyrType.Hovedsentral => "#1f2937",
        PlanUtstyrType.Kamera => "#4338ca",
        PlanUtstyrType.Dorautomatikk => "#0891b2",
        PlanUtstyrType.Sirene => "#f59e0b",
        PlanUtstyrType.Albuebryter => "#be185d",
        PlanUtstyrType.Nokkelbryter => "#a16207",
        PlanUtstyrType.Grensesnittboks => "#65a30d",
        PlanUtstyrType.Iq => "#be123c",
        PlanUtstyrType.Node => "#1e40af",
        PlanUtstyrType.Gateway => "#78716c",
        PlanUtstyrType.Hub => "#166534",
        _ => "#333333"
    };

    /// <summary>Innhold i en 24x24 viewBox som gir et lite symbol som ligner utstyret.</summary>
    public static string IkonSvg(this PlanUtstyrType type) => type switch
    {
        // Kortleser (basert på faktisk produktbilde av kortleser/kodepanel-terminal):
        // høy, smal, rundtoppet leserenhet med tapp-/leserfelt øverst og et lite
        // 3x3-tastatur under - matcher den karakteristiske "tårn"-fasongen.
        PlanUtstyrType.Kortleser =>
            "<rect x='7.5' y='2' width='9' height='20' rx='4' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<rect x='9.3' y='4.6' width='5.4' height='2.6' rx='1.3' fill='currentColor'/>" +
            "<circle cx='10.3' cy='10.6' r='0.7' fill='currentColor'/><circle cx='12' cy='10.6' r='0.7' fill='currentColor'/><circle cx='13.7' cy='10.6' r='0.7' fill='currentColor'/>" +
            "<circle cx='10.3' cy='13.3' r='0.7' fill='currentColor'/><circle cx='12' cy='13.3' r='0.7' fill='currentColor'/><circle cx='13.7' cy='13.3' r='0.7' fill='currentColor'/>" +
            "<circle cx='10.3' cy='16' r='0.7' fill='currentColor'/><circle cx='12' cy='16' r='0.7' fill='currentColor'/><circle cx='13.7' cy='16' r='0.7' fill='currentColor'/>",

        // KAC nødåpner (basert på faktisk bilde av grønn "Emergency Door Release"):
        // firkantet boks med rømningssymbol (løpende figur) øverst og en
        // trykknapp flankert av piler nederst.
        PlanUtstyrType.Kac =>
            "<rect x='3.5' y='3.5' width='17' height='17' rx='1.8' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<circle cx='12' cy='7.6' r='1.05' fill='currentColor'/>" +
            "<path d='M12 8.7 L11 11.3 M12 8.7 L13.3 10.9 M11.6 9.9 L9.6 9.1 M11.6 9.9 L12.9 8.5 M11 11.3 L9.8 14.2 M13.3 10.9 L14.4 13.8' stroke='currentColor' stroke-width='0.9' fill='none' stroke-linecap='round'/>" +
            "<line x1='5.7' y1='17.2' x2='8.3' y2='17.2' stroke='currentColor' stroke-width='1.1'/>" +
            "<path d='M8.3 17.2 L7 15.9 M8.3 17.2 L7 18.5' stroke='currentColor' stroke-width='1' fill='none' stroke-linecap='round'/>" +
            "<circle cx='12' cy='17.2' r='1.3' fill='currentColor'/>" +
            "<line x1='15.7' y1='17.2' x2='18.3' y2='17.2' stroke='currentColor' stroke-width='1.1'/>" +
            "<path d='M15.7 17.2 L17 15.9 M15.7 17.2 L17 18.5' stroke='currentColor' stroke-width='1' fill='none' stroke-linecap='round'/>",

        // Åpneknapp: flat rund tilgjengelighetsknapp (uten bakplate, i motsetning
        // til KAC), skiller seg tydelig fra nødåpneren over.
        PlanUtstyrType.Apneknapp =>
            "<circle cx='12' cy='12' r='8.6' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<circle cx='12' cy='12' r='4' fill='currentColor'/>",

        // Sentral (basert på faktisk bilde av ARX-1): helt lukket, flatt
        // veggskap med to synlige monteringsskruer - ingen falske display/lys.
        PlanUtstyrType.Sentral =>
            "<rect x='3.5' y='4.5' width='17' height='15' rx='2.5' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<circle cx='8' cy='8.3' r='0.75' fill='currentColor'/>" +
            "<circle cx='16' cy='8.3' r='0.75' fill='currentColor'/>",

        PlanUtstyrType.Stikkontakt =>
            "<rect x='5' y='4' width='14' height='16' rx='2' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<line x1='9' y1='9' x2='9' y2='13' stroke='currentColor' stroke-width='1.8'/>" +
            "<line x1='15' y1='9' x2='15' y2='13' stroke='currentColor' stroke-width='1.8'/>" +
            "<circle cx='12' cy='16.5' r='1.1' fill='currentColor'/>",

        PlanUtstyrType.Ups =>
            "<rect x='3' y='7' width='16' height='10' rx='1.5' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<rect x='19.5' y='10' width='2' height='4' fill='currentColor'/>" +
            "<path d='M12.5 8.5 L9 13 H11.5 L10.5 15.5 L15 11 H12.5 Z' fill='currentColor'/>",

        PlanUtstyrType.Datapunkt =>
            "<path d='M7 5 H17 V13 L14 17 H10 L7 13 Z' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<line x1='9.5' y1='5' x2='9.5' y2='9' stroke='currentColor' stroke-width='1.3'/>" +
            "<line x1='12' y1='5' x2='12' y2='9' stroke='currentColor' stroke-width='1.3'/>" +
            "<line x1='14.5' y1='5' x2='14.5' y2='9' stroke='currentColor' stroke-width='1.3'/>",

        // PIR-detektor: sensordome med tre nedovergående detekteringsstråler.
        PlanUtstyrType.Pir =>
            "<path d='M5 15.5 Q12 5.5 19 15.5' fill='none' stroke='currentColor' stroke-width='1.7'/>" +
            "<line x1='4' y1='15.5' x2='20' y2='15.5' stroke='currentColor' stroke-width='1.7'/>" +
            "<path d='M8 15.5 L6.5 11.5 M12 15.5 L12 10.7 M16 15.5 L17.5 11.5' fill='none' stroke='currentColor' stroke-width='1.2'/>",

        // Alarmpanel (basert på faktisk bilde av veggmontert betjeningspanel):
        // liggende panel med display øverst og 4x-tastatur under.
        PlanUtstyrType.Alarmpanel =>
            "<rect x='2.5' y='4.5' width='19' height='15' rx='2' fill='none' stroke='currentColor' stroke-width='1.5'/>" +
            "<rect x='4.7' y='6.5' width='14.6' height='3.6' rx='0.6' fill='currentColor'/>" +
            "<circle cx='7' cy='13.3' r='0.75' fill='currentColor'/><circle cx='10.3' cy='13.3' r='0.75' fill='currentColor'/><circle cx='13.6' cy='13.3' r='0.75' fill='currentColor'/><circle cx='16.9' cy='13.3' r='0.75' fill='currentColor'/>" +
            "<circle cx='7' cy='16.7' r='0.75' fill='currentColor'/><circle cx='10.3' cy='16.7' r='0.75' fill='currentColor'/><circle cx='13.6' cy='16.7' r='0.75' fill='currentColor'/><circle cx='16.9' cy='16.7' r='0.75' fill='currentColor'/>",

        // Hovedsentral (basert på faktisk bilde av ARX 9016): høyere og
        // smalere lukket skap enn Sentral, med luftespalter øverst.
        PlanUtstyrType.Hovedsentral =>
            "<rect x='6.5' y='1.5' width='11' height='21' rx='2.5' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<line x1='8.5' y1='4' x2='9.7' y2='4' stroke='currentColor' stroke-width='1'/>" +
            "<line x1='11' y1='4' x2='12.2' y2='4' stroke='currentColor' stroke-width='1'/>" +
            "<line x1='13.5' y1='4' x2='14.7' y2='4' stroke='currentColor' stroke-width='1'/>",

        // Kamera: veggmontert kamerahus med linse (pupill) og festearm.
        PlanUtstyrType.Kamera =>
            "<line x1='6' y1='9' x2='6' y2='6' stroke='currentColor' stroke-width='1.3'/>" +
            "<rect x='2.5' y='9' width='12' height='7' rx='2' fill='none' stroke='currentColor' stroke-width='1.5'/>" +
            "<circle cx='8.5' cy='12.5' r='2.3' fill='none' stroke='currentColor' stroke-width='1.4'/>" +
            "<circle cx='8.5' cy='12.5' r='0.9' fill='currentColor'/>" +
            "<path d='M14.5 11 L20 8.5 V16.5 L14.5 14 Z' fill='currentColor'/>",

        // Dørautomatikk (basert på faktisk bilde av ED100): slank overliggende
        // operatørboks med den karakteristiske knekte glidearmen ut fra enden.
        PlanUtstyrType.Dorautomatikk =>
            "<rect x='2' y='3' width='20' height='4.5' rx='1.2' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<path d='M4 7.5 L11 14 L19 10.8' fill='none' stroke='currentColor' stroke-width='1.4' stroke-linecap='round' stroke-linejoin='round'/>" +
            "<circle cx='11' cy='14' r='1' fill='currentColor'/>",

        // Sirene (basert på faktisk bilde av utendørs alarmsirene): liggende
        // boks med rund horn-/høyttalergrill og luftespalter til høyre.
        PlanUtstyrType.Sirene =>
            "<rect x='2' y='6' width='20' height='12' rx='1.5' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<circle cx='8.3' cy='12' r='4.1' fill='none' stroke='currentColor' stroke-width='1.1'/>" +
            "<circle cx='8.3' cy='12' r='2.4' fill='none' stroke='currentColor' stroke-width='1'/>" +
            "<circle cx='8.3' cy='12' r='0.85' fill='currentColor'/>" +
            "<line x1='13.8' y1='9' x2='18.5' y2='9' stroke='currentColor' stroke-width='0.9'/>" +
            "<line x1='13.8' y1='11.2' x2='18.5' y2='11.2' stroke='currentColor' stroke-width='0.9'/>" +
            "<line x1='13.8' y1='13.4' x2='18.5' y2='13.4' stroke='currentColor' stroke-width='0.9'/>" +
            "<line x1='13.8' y1='15.6' x2='18.5' y2='15.6' stroke='currentColor' stroke-width='0.9'/>",

        // Albuebryter: firkantet plate med stort ovalt trykkfelt (betjenes med
        // albuen), tydelig bredere og flatere enn den runde åpneknappen.
        PlanUtstyrType.Albuebryter =>
            "<rect x='4' y='4' width='16' height='16' rx='2' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<rect x='6.6' y='7.5' width='10.8' height='9' rx='4.5' fill='currentColor'/>",

        // Nøkkelbryter (basert på faktisk bilde av veggmontert nøkkelbryter):
        // firkantet boks med rund sylinder i midten og pil som viser dreieretning.
        PlanUtstyrType.Nokkelbryter =>
            "<rect x='4' y='4' width='16' height='16' rx='3' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<path d='M9.3 8.3 A4.2 4.2 0 0 1 14.3 7.6' fill='none' stroke='currentColor' stroke-width='1.2'/>" +
            "<path d='M14.3 7.6 L15.3 7.1 M14.3 7.6 L13.7 8.5' stroke='currentColor' stroke-width='1' fill='none' stroke-linecap='round'/>" +
            "<circle cx='12' cy='13.5' r='3.4' fill='currentColor'/>",

        // Grensesnittboks: liten koblingsboks med klemmerekke (terminalpunkter).
        PlanUtstyrType.Grensesnittboks =>
            "<rect x='5' y='6' width='14' height='12' rx='1.5' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<circle cx='9' cy='12' r='1' fill='currentColor'/><circle cx='12' cy='12' r='1' fill='currentColor'/><circle cx='15' cy='12' r='1' fill='currentColor'/>",

        // IQ-lås: trådløst dørhåndtak/lås med innebygd elektronikk-indikator.
        PlanUtstyrType.Iq =>
            "<rect x='4' y='4' width='16' height='16' rx='2' fill='none' stroke='currentColor' stroke-width='1.5'/>" +
            "<path d='M8 9 L8 15 L14 15' fill='none' stroke='currentColor' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'/>" +
            "<circle cx='16' cy='8.5' r='1.1' fill='currentColor'/>",

        // Trådløs node: veggboks som sender/mottar radiosignal (buede antennebuer).
        PlanUtstyrType.Node =>
            "<rect x='6' y='8.5' width='12' height='9.5' rx='2' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<path d='M9 8.5 Q12 4 15 8.5' fill='none' stroke='currentColor' stroke-width='1.3'/>" +
            "<path d='M7 8.5 Q12 1 17 8.5' fill='none' stroke='currentColor' stroke-width='1'/>" +
            "<circle cx='12' cy='13.3' r='1.1' fill='currentColor'/>",

        // Gateway: boks med trekantet nettverksgraf (kobler system mot nettverk/sky).
        PlanUtstyrType.Gateway =>
            "<rect x='4' y='5' width='16' height='14' rx='2' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<line x1='12' y1='9.5' x2='8.5' y2='15' stroke='currentColor' stroke-width='1.1'/>" +
            "<line x1='12' y1='9.5' x2='15.5' y2='15' stroke='currentColor' stroke-width='1.1'/>" +
            "<circle cx='12' cy='9.5' r='1.3' fill='currentColor'/><circle cx='8.5' cy='15' r='1.3' fill='currentColor'/><circle cx='15.5' cy='15' r='1.3' fill='currentColor'/>",

        // Hub/switch: boks med portspor og statuslamper - unngår kryssmønster
        // som kan forveksles med et "avbryt"-ikon.
        PlanUtstyrType.Hub =>
            "<rect x='3.5' y='5' width='17' height='13' rx='1.5' fill='none' stroke='currentColor' stroke-width='1.6'/>" +
            "<circle cx='7.2' cy='9' r='0.7' fill='currentColor'/><circle cx='12' cy='9' r='0.7' fill='currentColor'/><circle cx='16.8' cy='9' r='0.7' fill='currentColor'/>" +
            "<rect x='6' y='13' width='2.4' height='2.8' fill='currentColor'/>" +
            "<rect x='10.8' y='13' width='2.4' height='2.8' fill='currentColor'/>" +
            "<rect x='15.6' y='13' width='2.4' height='2.8' fill='currentColor'/>",

        _ => ""
    };
}
