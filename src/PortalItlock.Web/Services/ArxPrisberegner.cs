namespace PortalItlock.Web.Services;

// ARX-kalkulatoren: regner ut hardware, strøm, batteri og lisenser ut fra
// antall dører og valgene i veiviseren. Artikkelnumre, kapasiteter og
// strømforbruk (mA ved 24 V) er hentet fra ASSA ABLOYs nettbutikk og
// datablad (november 2026). Prisene kommer utenfra, fra komponentregisteret.

public enum ArxSentral { Auto, Ip9101, Power9101S, Power9008, Voco9016 }
public enum ArxStrom9101 { Batteribackup, PoE }
public enum ArxPsc { Auto, MII, L }

public sealed record ArxLeser(string Kode, string Navn, string Tekst, int HvileMa, int MaksMa, bool TrengerDac, bool HarLasSelv);
public sealed record ArxLasType(string Nokkel, string Navn, string Kilde, int HvileMa, int MaksMa, bool EgneVerdier = false);
public sealed record ArxModul(string Navn, string Kode);

public sealed class ArxValg
{
    public int Dorer { get; set; } = 5;
    public string Leser { get; set; } = "secure";
    public bool BeggeSider { get; set; }
    public string Knapp { get; set; } = "";
    public Dictionary<string, int> Laser { get; set; } = new() { ["slutt"] = -1 }; // -1 = følg antall dører
    public int EgenLasHvile { get; set; }
    public int EgenLasMaks { get; set; } = 250;
    public ArxSentral Sentral { get; set; } = ArxSentral.Auto;
    public int Timer { get; set; } = 12;
    public ArxStrom9101 Strom9101 { get; set; } = ArxStrom9101.Batteribackup;
    public ArxPsc Psc { get; set; } = ArxPsc.Auto;
    public decimal Aldring { get; set; } = 0.8m;
    public bool Utvidelse { get; set; }
    public string Grunnpakke { get; set; } = ArxPrisberegner.PakkeBasis;
    public int Klienter { get; set; }
    public bool Tilleggsfunksjoner { get; set; }
    public HashSet<string> Moduler { get; set; } = [];
    public bool MobileAccess { get; set; }
    public bool Alarm { get; set; }
    public int Alarmgrad { get; set; } = 2;
    public int InngangerPerDor { get; set; } = 2;
    public int EkstraInnganger { get; set; } = 4;
    public int Paneler { get; set; } = 1;
    public bool Communicator { get; set; }
    public int Brikker { get; set; } = 20;

    public int LasAntall(string nokkel) =>
        Laser.TryGetValue(nokkel, out var n) ? (n < 0 ? Dorer : n) : 0;
}

public sealed record ArxLinje(string Gruppe, string Kode, string Navn, int Antall, string Hvorfor);
public sealed record ArxStromRad(string Navn, decimal HvileMa, decimal MaksMa);

public sealed class ArxEnhet
{
    public required string Navn { get; init; }
    public int Dorer { get; init; }
    public int Plasser { get; init; }
    public int Dac { get; set; }
    public decimal EgenMa { get; init; }
    public string Type { get; init; } = "power"; // power, poe, ps, psc
    public bool Bare20Ah { get; init; }
    public List<ArxStromRad> Rader { get; } = [];
    public decimal HvileMa => Rader.Sum(r => r.HvileMa);
    public decimal MaksMa => Rader.Sum(r => r.MaksMa);
    public decimal BehovAh { get; set; }
    public decimal KapasitetAh { get; set; }
    public decimal LastA { get; set; }
    public string Batteri { get; set; } = "";
    public bool ForLite { get; set; }
    public bool LastOk => MaksMa / 1000m <= LastA;
}

public sealed record ArxResultat(
    ArxSentral Sentral,
    ArxSentral Anbefalt,
    IReadOnlyList<ArxLinje> Linjer,
    IReadOnlyList<ArxEnhet> Enheter,
    IReadOnlyList<(bool Feil, string Tekst)> Advarsler,
    int DekketeDorlisenser);

public static class ArxPrisberegner
{
    public const string PakkeBasis = "S559900000996";
    public const string PakkeStandard = "S559901000996";
    public const string PakkeAvansert = "S559902000996";
    public const string PakkeKomplett = "S559903000996";

    public static readonly (string Kode, string Navn, bool Bekreftet)[] Grunnpakker =
    [
        (PakkeBasis, "ARX Basis", false), (PakkeStandard, "ARX Standard", false),
        (PakkeAvansert, "ARX Avansert", true), (PakkeKomplett, "ARX Komplett", false)
    ];

    public static readonly Dictionary<string, ArxLeser> Lesere = new()
    {
        ["secure"] = new("S559885J084", "Pando Secure GO", "Tastatur, PIN og mobil (BLE)", 50, 85, true, false),
        ["display"] = new("S559886J085", "Pando Display GO", "Med display, også alarmpanel", 50, 85, true, false),
        ["mini"] = new("S559883084", "Pando Mini GO", "Uten tastatur, for smal karm", 20, 40, true, false),
        ["dbl835"] = new("S556724835C50GO", "DBL362-835C50Go", "Dørbladleser, lås og DAC innebygd", 70, 138, false, true),
        ["dbl815"] = new("S556726815C50GO", "DBL362T-815C50Go", "Dørbladleser, lås og DAC innebygd", 70, 138, false, true),
    };

    // Åpneknapper: nøkkel -> (kode, navn, tekst, hvile, maks)
    public static readonly Dictionary<string, (string Kode, string Navn, string Tekst, int Hvile, int Maks)> Knapper = new()
    {
        ["touchhio"] = ("PANDO-TOUCH-HIO", "Pando Touch Hi-O", "Berøringsknapp på Hi-O-bussen, med summer", 25, 25),
        ["touchre"] = ("PANDO-TOUCH-RE", "Pando Touch RE", "Berøringsknapp med reléutgang", 3, 36),
        ["elko"] = ("ELKO-RS16", "Elko RS16 påveggsbryter", "Vanlig mekanisk bryter. Trenger impulsfjær T47", 0, 0),
    };
    public const string Impulsfjaer = "227T47821";

    // Snitt fra produsentenes datablad, mA ved 24 V [i hvile, ved åpning].
    // El-sluttstykke/solenoid: 17 StepLock-modeller (STEP 15-140, rettvendt) -> 0/250.
    // Motorlås: STEP 300-serien 69/170, STEP 535/550/551 32/255, ASSA 840C/851C 90/510 -> 65/310.
    // Magnetlås: ASSA (MG500M, 827A, Maxi shear, 1200 lbs) ~250 og dormakaba (EM 300, EMG 300 M) ~190 -> 220.
    // BFS SCP 111 nødåpner: produktark, 24 V standby 11 mA, operativ 41 mA.
    public static readonly ArxLasType[] LasTyper =
    [
        new("slutt", "El-sluttstykke / solenoid", "Snitt av 17 StepLock-modeller (STEP 15–140)", 0, 250),
        new("motor", "Motorlås", "Snitt av STEP 300- og 500-serien og ASSA 840C/851C", 65, 310),
        new("magnet", "Magnetlås", "Snitt av ASSA og dormakaba. Trekker strøm hele tiden den er låst", 220, 220),
        new("scp111", "Nødåpner BFS SCP 111 (lys og lyd)", "Art.nr. 100686. Produktark: standby 11 mA, operativ 41 mA", 11, 41),
        new("egen", "Annen lås", "Egne verdier fra låsens datablad", 0, 0, EgneVerdier: true),
    ];

    public static readonly ArxModul[] Moduler =
    [
        new("Import / Eksport", "S559030000996"), new("Domener", "S559041996"), new("Sonekontroll", "S559006996"),
        new("Anti-passback", "S559005996"), new("Sluse", "S559007996"), new("Heis", "S559004996"),
        new("Booking", "S559035996"), new("Porttelefon", "S559039996"), new("Galaxy", "S559012996"),
        new("Galaxy VRIO", "S559013996"), new("Trigger", "S559034996"), new("Bildelogg", "S559047996"),
        new("IDExpert", "S559053996"), new("ARX Go", "S55940000996"), new("Mine sider", "S559032996"),
        new("Badge Creator", "S559048996"), new("CLIQ i ARX", "S5590500996"),
    ];

    public static readonly (int Antall, string Kode)[] Dorlisenser =
    [
        (1, "S559701996"), (4, "S559704996"), (8, "S559708996"), (16, "S559716996"), (32, "S559732996"),
        (40, "S559740996"), (50, "S559750996"), (60, "S559760996"), (80, "S559780996"), (100, "S559810996"),
        (200, "S559820996"), (300, "S559830996"), (400, "S559840996"),
    ];

    public static readonly (int Antall, string Kode)[] Alarminnganger =
    [
        (10, "S55940010996"), (20, "S55940020996"), (50, "S55940050996"), (100, "S55940100996"),
        (200, "S55940200996"), (500, "S55940500996"), (1000, "S55941000996"),
    ];

    // Standardnavn når artikkelen ikke finnes i komponentregisteret.
    public static readonly Dictionary<string, string> Navn = new()
    {
        ["S55299101165S"] = "ARX Power 9101S", ["S55910142085"] = "ARX 9101 IV2 endørsentral", ["S5591013160"] = "9101D3 tilleggskort, +3 dører",
        ["S55299008V165"] = "ARX Power Voco 9008", ["S5540144160"] = "Dørkort 4014LC V, +4 dører", ["S5590163V084"] = "ARX Voco 9016V",
        ["S55299203165"] = "3PS batteribackup inkl. 2 × 2,3 Ah", ["S55299205165"] = "5PS batteribackup 24 V 5 A", ["S55299210165"] = "10PS batteribackup 24 V 10 A",
        ["S55299200165"] = "Batteriskap til 5PS/10PS", ["S55299310264"] = "10PSC M II batteribackup 10 A", ["S55299310165"] = "10PSC L batteribackup 10 A",
        ["S55299300165"] = "Batteriskap for PSC", ["S5529X00B165"] = "ARX Power skap", ["S5529X00F160"] = "ARX Power Fuse 05 sikringskort",
        ["S5590164N160"] = "9016III MIO6-6 master", ["S5590000160WOB"] = "ARX Communicator",
        ["S511107084"] = "Batteri UPLUS 12V 7,2Ah", ["S511104084"] = "Batteri UPLUS 12V 20Ah", ["BATT14"] = "Batteri 12V 14Ah", ["BATT45"] = "Batteri UPLUS 12V 45Ah",
        ["S55666300085"] = "ARX DAC630 Hi-O dørsentral", ["S5566504000"] = "ARX DAC630 + SIO6-4 dørsentral",
        ["S559885J084"] = "Pando Secure GO", ["S559886J085"] = "Pando Display GO", ["S559883084"] = "Pando Mini GO",
        ["S556724835C50GO"] = "DBL362-835C50Go dørbladleser", ["S556726815C50GO"] = "DBL362T-815C50Go dørbladleser",
        ["PANDO-TOUCH-HIO"] = "Pando Touch Hi-O åpneknapp", ["PANDO-TOUCH-RE"] = "Pando Touch RE åpneknapp",
        ["ELKO-RS16"] = "Elko RS16 påveggsbryter, hvit 1-pol IP20", [Impulsfjaer] = "Impulsfjær T47 til Elko RS16",
        ["ARX-BRIKKE"] = "DESFire-brikke", ["S559020996"] = "Ekstra klientlisens", ["S5590490996"] = "ARX Mobile Access",
        [PakkeBasis] = "ARX grunnpakke Basis", [PakkeStandard] = "ARX grunnpakke Standard",
        [PakkeAvansert] = "ARX grunnpakke Avansert", [PakkeKomplett] = "ARX grunnpakke Komplett",
    };

    public const int DacMa = 35;

    public static readonly Dictionary<ArxSentral, (string Navn, string Tekst, int Maks)> Sentraler = new()
    {
        [ArxSentral.Ip9101] = ("9101 IV2", "Endørsentral. Leseren kobles rett på sentralen. Opptil 4 dører med 9101D3.", 4),
        [ArxSentral.Power9008] = ("Power Voco 9008", "Sentral med batteri i samme skap. 4 dører, 8 med dørkort 4014LC V. Opptil alarmgrad 4.", 8),
        [ArxSentral.Voco9016] = ("Voco 9016V", "Sentral for opptil 16 dører (4 + tre dørkort). Får egen batteribackup PSC.", 16),
        [ArxSentral.Power9101S] = ("Power 9101S", "For mindre satellittinstallasjoner og dedikert alarm: 9101 + SIO6-4 + batteri i ett skap. Alarmgrad 2.", 4),
    };

    public static ArxSentral Anbefalt(int dorer) => dorer <= 1 ? ArxSentral.Ip9101 : dorer <= 8 ? ArxSentral.Power9008 : ArxSentral.Voco9016;

    public static string Navnet(string kode) => Navn.TryGetValue(kode, out var n) ? n : kode;

    public static IEnumerable<string> AlleKoder() =>
        Navn.Keys
            .Concat(Dorlisenser.Select(d => d.Kode))
            .Concat(Alarminnganger.Select(a => a.Kode))
            .Concat(Moduler.Select(m => m.Kode))
            .Distinct();

    // Fordel dører jevnt på enheter med gitt maks.
    public static List<int> Fordel(int dorer, int maks)
    {
        var n = (int)Math.Ceiling(dorer / (double)maks);
        var b = dorer / n;
        var r = dorer % n;
        return Enumerable.Range(0, n).Select(i => b + (i < r ? 1 : 0)).ToList();
    }

    // Minste kombinasjon av pakker som dekker n: minst overskudd, så færrest pakker.
    public static List<(int Antall, string Kode)> Dorlisenskombinasjon(int n)
    {
        if (n <= 0) return [];
        var maks = n + Dorlisenser[^1].Antall;
        var best = new List<(int, string)>?[maks + 1];
        best[0] = [];
        for (var s = 1; s <= maks; s++)
        {
            foreach (var p in Dorlisenser)
            {
                if (p.Antall > s || best[s - p.Antall] is not { } f) continue;
                if (best[s] is null || f.Count + 1 < best[s]!.Count) best[s] = [.. f, p];
            }
        }
        for (var s = n; s <= maks; s++) if (best[s] is { } k) return k;
        return [];
    }

    private sealed record Kandidat(string Navn, decimal A, decimal Ah, (string Kode, int Antall, string Hvorfor)[] Linjer);

    public static ArxResultat Beregn(ArxValg v)
    {
        var d = Math.Clamp(v.Dorer, 1, 400);
        var anbefalt = Anbefalt(d);
        var s = v.Sentral == ArxSentral.Auto ? anbefalt : v.Sentral;
        if ((s is ArxSentral.Ip9101 or ArxSentral.Power9101S) && d > 4) s = anbefalt;

        var les = Lesere.TryGetValue(v.Leser, out var l0) ? l0 : Lesere["secure"];
        var knapp = Knapper.TryGetValue(v.Knapp, out var k0) ? k0 : default;
        var harKnapp = knapp.Kode is not null;
        var lesAnt = v.BeggeSider ? 2 : 1;

        var linjer = new List<ArxLinje>();
        var adv = new List<(bool, string)>();
        void Add(string g, string kode, int ant, string hvorfor) { if (ant > 0) linjer.Add(new(g, kode, Navnet(kode), ant, hvorfor)); }

        // Sentraler
        var enheter = new List<ArxEnhet>();
        switch (s)
        {
            case ArxSentral.Power9101S:
                Add("Sentral", "S55299101165S", 1, "Første dør kobles rett på sentralen");
                if (d > 1) Add("Sentral", "S5591013160", 1, $"Dør 2–{d} kobles via DAC630");
                enheter.Add(new ArxEnhet { Navn = "ARX Power 9101S", Dorer = d, Plasser = 4, EgenMa = 251, Type = "power", Bare20Ah = true, Dac = d - 1 });
                break;
            case ArxSentral.Ip9101:
                Add("Sentral", "S55910142085", 1, "Første dør kobles rett på sentralen");
                if (d > 1) Add("Sentral", "S5591013160", 1, $"Dør 2–{d} kobles via DAC630");
                var poe = v.Strom9101 == ArxStrom9101.PoE;
                enheter.Add(new ArxEnhet { Navn = "ARX 9101 IV2", Dorer = d, Plasser = d > 1 ? 4 : 1, EgenMa = 120, Type = poe ? "poe" : "ps", Dac = d - 1 });
                if (poe) adv.Add((false, "PoE gir ingen batteribackup i sentralen. PoE-svitsjen bør stå på UPS. Maks 1 A ut fra 9101."));
                break;
            case ArxSentral.Power9008:
            {
                var f = Fordel(d, 8);
                Add("Sentral", "S55299008V165", f.Count, f.Count > 1 ? $"Maks 8 dører per sentral, fordelt {string.Join(" + ", f)}" : "");
                Add("Sentral", "S5540144160", f.Count(x => x > 4), "Når en 9008 har mer enn 4 dører");
                for (var i = 0; i < f.Count; i++)
                    enheter.Add(new ArxEnhet { Navn = "ARX Power Voco 9008" + (f.Count > 1 ? $" nr {i + 1}" : ""), Dorer = f[i], Plasser = f[i] > 4 ? 8 : 4, EgenMa = 225, Type = "power", Dac = f[i] });
                break;
            }
            default:
            {
                var f = Fordel(d, 16);
                Add("Sentral", "S5590163V084", f.Count, f.Count > 1 ? $"Maks 16 dører per sentral, fordelt {string.Join(" + ", f)}" : "");
                Add("Sentral", "S5540144160", f.Sum(x => Math.Max(0, (int)Math.Ceiling(x / 4.0) - 1)), "Grunnkortet tar 4 dører, ett dørkort per 4 ekstra");
                for (var i = 0; i < f.Count; i++)
                    enheter.Add(new ArxEnhet { Navn = "ARX Voco 9016V" + (f.Count > 1 ? $" nr {i + 1}" : ""), Dorer = f[i], Plasser = (int)Math.Ceiling(f[i] / 4.0) * 4, EgenMa = 100, Type = "psc", Dac = f[i] });
                break;
            }
        }
        if (!les.TrengerDac) foreach (var e in enheter) e.Dac = 0;

        // Dør
        var dacAnt = enheter.Sum(e => e.Dac);
        Add("Dør", v.Alarm ? "S5566504000" : "S55666300085", dacAnt,
            v.Alarm ? "Én per dør, med SIO6-4 for alarminnganger" : (s is ArxSentral.Ip9101 or ArxSentral.Power9101S) && d > 1 ? "Én per dør utenom den første" : "Én per dør");
        Add("Dør", les.Kode, d * lesAnt, v.BeggeSider ? "To per dør, inn og ut" : "Én per dør");
        if (harKnapp) Add("Dør", knapp.Kode, d, "Én per dør");
        if (v.Knapp == "elko") Add("Dør", Impulsfjaer, d, "Gjør bryteren fjærende, én per bryter");

        // Låser på sentralens strøm
        var laser = les.HarLasSelv ? [] : LasTyper
            .Select(t => (Type: t, Antall: v.LasAntall(t.Nokkel),
                Hvile: t.EgneVerdier ? v.EgenLasHvile : t.HvileMa, Maks: t.EgneVerdier ? v.EgenLasMaks : t.MaksMa))
            .Where(x => x.Antall > 0).ToList();

        var perDorHvile = les.HvileMa * lesAnt + (harKnapp ? knapp.Hvile : 0);
        var perDorMaks = les.MaksMa * lesAnt + (harKnapp ? knapp.Maks : 0);

        foreach (var e in enheter)
        {
            e.Rader.Add(new("Sentralen selv", e.EgenMa, e.EgenMa));
            if (e.Dac > 0) e.Rader.Add(new($"DAC630 × {e.Dac}", e.Dac * DacMa, e.Dac * DacMa));
            e.Rader.Add(new($"{les.Navn}{(v.BeggeSider ? " × 2" : "")}{(harKnapp ? " + knapp" : "")}, {e.Dorer} {(e.Dorer == 1 ? "dør" : "dører")}", perDorHvile * e.Dorer, perDorMaks * e.Dorer));
            var andel = e.Dorer / (decimal)d;
            foreach (var x in laser)
            {
                var n = x.Antall * andel;
                e.Rader.Add(new($"{x.Type.Navn} × {Math.Round(n, 1)}", x.Hvile * n, x.Maks * n));
            }
            if (v.Alarm && v.Communicator && e.Type != "poe") e.Rader.Add(new("ARX Communicator", 51, 55));

            e.BehovAh = e.HvileMa / 1000m * v.Timer / (v.Aldring <= 0 ? 0.8m : v.Aldring);
            VelgStrom(v, e, Add, adv);
        }
        Add("Strøm", "S5529X00F160", enheter.Count(e => e.Type is "power" or "psc"), "Sikringskort, ett per sentral");

        // Alarm
        if (v.Alarm)
        {
            if (v.Alarmgrad >= 3)
            {
                if (s is ArxSentral.Ip9101 or ArxSentral.Power9101S) adv.Add((true, "9101 er godkjent til alarmgrad 2. Velg 9008 eller 9016V for grad 3."));
                else Add("Alarm", "S5590164N160", enheter.Count, "Alarmgrad 3 krever MIO6-6 per sentral");
            }
            Add("Alarm", "S559886J085", v.Paneler, "Betjeningspanel");
            if (v.Communicator) Add("Alarm", "S5590000160WOB", enheter.Count(e => e.Type != "poe"), "Én per sentral");
        }

        // Programvare og lisenser
        if (!v.Utvidelse) Add("Programvare", v.Grunnpakke, 1, "Én per anlegg");
        Add("Programvare", "S559020996", v.Klienter, "");
        var kombo = Dorlisenskombinasjon(d);
        var dekk = kombo.Sum(k => k.Antall);
        foreach (var g in kombo.GroupBy(k => k))
            linjer.Add(new("Lisenser", g.Key.Kode, $"ARX dørlisens {g.Key.Antall} {(g.Key.Antall == 1 ? "dør" : "dører")}", g.Count(),
                $"{d} dører dekkes av {string.Join(" + ", kombo.Select(k => k.Antall))}"));
        if (v.Alarm)
        {
            var inn = d * v.InngangerPerDor + v.EkstraInnganger;
            if (inn > 0)
            {
                var p = Alarminnganger.FirstOrDefault(a => a.Antall >= inn);
                if (p.Kode is null) p = Alarminnganger[^1];
                linjer.Add(new("Lisenser", p.Kode, $"ARX alarm {p.Antall} innganger", 1, $"{inn} innganger trengs"));
            }
        }
        if (v.Tilleggsfunksjoner)
        {
            foreach (var m in Moduler.Where(m => v.Moduler.Contains(m.Kode)))
                linjer.Add(new("Lisenser", m.Kode, "ARX " + m.Navn.Replace("ARX ", ""), 1, ""));
            if (v.MobileAccess) Add("Lisenser", "S5590490996", 1, "");
        }
        Add("Kort", "ARX-BRIKKE", v.Brikker, "");

        var rekke = new[] { "Sentral", "Strøm", "Dør", "Alarm", "Programvare", "Lisenser", "Kort" };
        var sortert = linjer.OrderBy(x => Array.IndexOf(rekke, x.Gruppe)).ToList();
        return new ArxResultat(s, anbefalt, sortert, enheter, adv, dekk);
    }

    // Regler: velg den minste løsningen som både dekker batteribehovet (Ah) og tåler maks last (A).
    private static void VelgStrom(ArxValg v, ArxEnhet e, Action<string, string, int, string> add, List<(bool, string)> adv)
    {
        var tA = e.MaksMa / 1000m;
        List<Kandidat> kand = e.Type switch
        {
            "ps" =>
            [
                new("3PS, 2 × 2,3 Ah", 3, 2.3m, [("S55299203165", 1, "24 V 3 A, batterier følger med")]),
                new("5PS, 2 × 7,2 Ah", 5, 7.2m, [("S55299205165", 1, "24 V 5 A"), ("S511107084", 2, "To batterier i serie")]),
                new("5PS, 2 × 14 Ah", 5, 14, [("S55299205165", 1, "24 V 5 A"), ("BATT14", 2, "To batterier i serie")]),
                new("10PS, 2 × 14 Ah", 10, 14, [("S55299210165", 1, "24 V 10 A"), ("BATT14", 2, "To batterier i serie")]),
                new("10PS + batteriskap, 6 × 14 Ah", 10, 42, [("S55299210165", 1, "24 V 10 A"), ("S55299200165", 1, "Batteriskap til 5PS/10PS"), ("BATT14", 6, "2 i 10PS og 4 i batteriskap")]),
            ],
            "power" =>
            [
                .. e.Bare20Ah ? Array.Empty<Kandidat>() : [new Kandidat("2 × 7,2 Ah", 4, 7.2m, [("S511107084", 2, "To batterier i serie")])],
                new("2 × 20 Ah", 4, 20, [("S511104084", 2, "To batterier i serie")]),
                new("4 × 20 Ah med Power skap", 4, 40, [("S511104084", 4, "2 i sentralen og 2 i Power skap"), ("S5529X00B165", 1, "Plass til to ekstra batterier")]),
            ],
            "psc" => PscKandidater(v.Psc),
            _ => [],
        };

        if (kand.Count == 0)
        {
            e.Batteri = "PoE, ingen batteri";
            e.LastA = 1;
            return;
        }

        var valgt = kand.FirstOrDefault(k => k.Ah >= e.BehovAh && k.A >= tA) ?? kand[^1];
        e.Batteri = valgt.Navn;
        e.LastA = valgt.A;
        e.KapasitetAh = valgt.Ah;
        for (var i = 0; i < valgt.Linjer.Length; i++)
        {
            var (kode, ant, hvorfor) = valgt.Linjer[i];
            add("Strøm", kode, ant, i == 0 ? $"{e.Navn}: {e.BehovAh:0.0} Ah og {tA:0.00} A trengs. {hvorfor}" : hvorfor);
        }
        if (valgt.Ah < e.BehovAh)
        {
            e.ForLite = true;
            adv.Add((true, $"{e.Navn} trenger {e.BehovAh:0.0} Ah, mer enn {valgt.Ah:0.#} Ah i største løsning ({valgt.Navn}). Kort ned driftstiden, flytt låser til egen strømforsyning, eller del opp anlegget."));
        }
        if (!e.LastOk)
            adv.Add((true, $"{e.Navn}: {tA:0.00} A er over grensen på {e.LastA:0.#} A. Del opp, eller gi låsene egen strømforsyning."));
    }

    private static List<Kandidat> PscKandidater(ArxPsc psc)
    {
        var m = new Kandidat("10PSC M II, 2 × 20 Ah", 10, 20, [("S55299310264", 1, "24 V 10 A, overvåkes i ARX"), ("S511104084", 2, "To batterier i serie")]);
        var ls = Enumerable.Range(0, 5).Select(bb => new Kandidat(
            bb == 0 ? "10PSC L, 2 × 45 Ah" : $"10PSC L + {bb} batteriskap, {2 + 2 * bb} × 45 Ah", 10, 45 * (1 + bb),
            bb == 0
                ? [("S55299310165", 1, "24 V 10 A, overvåkes i ARX"), ("BATT45", 2, "To batterier i serie")]
                : [("S55299310165", 1, "24 V 10 A, overvåkes i ARX"), ("S55299300165", bb, "Batteriskap for PSC, 2 × 45 Ah hver"), ("BATT45", 2 + 2 * bb, "To batterier per skap")])).ToList();
        return psc switch { ArxPsc.MII => [m], ArxPsc.L => ls, _ => [m, .. ls] };
    }
}
