namespace PortalItlock.Web.Models;

// ---------- Priskoder: arbeid og tillegg som linjer ----------
//
// Timer, kjøring, småmateriell, rigg og drift osv. som linjer med kode på
// tilbudet, på samme måte som varer. Kodene settes opp én gang (Priskoder),
// med samme nummer som varenummeret i Tripletex, og hver linje kan få egen
// kostpris og salgspris. Se Services/KodeLinjeBeregner.cs.

public enum PrisKodeType
{
    Arbeid = 0,
    Kjoring = 1,
    Materiell = 2,
    Tillegg = 3,
    Annet = 4
}

public enum PrisKodeEnhet
{
    Time = 0,
    Km = 1,
    Stk = 2,
    Dag = 3,
    // Prosent av varenes utpris (for eksempel småmateriell).
    ProsentVarer = 4,
    // Prosent av arbeidslinjene (for eksempel rigg og drift).
    ProsentArbeid = 5
}

public static class PrisKodeTekst
{
    public static string Navn(this PrisKodeType t) => t switch
    {
        PrisKodeType.Arbeid => "Arbeid",
        PrisKodeType.Kjoring => "Kjøring",
        PrisKodeType.Materiell => "Materiell",
        PrisKodeType.Tillegg => "Tillegg",
        _ => "Annet"
    };

    public static string Navn(this PrisKodeEnhet e) => e switch
    {
        PrisKodeEnhet.Time => "time",
        PrisKodeEnhet.Km => "km",
        PrisKodeEnhet.Stk => "stk",
        PrisKodeEnhet.Dag => "dag",
        PrisKodeEnhet.ProsentVarer => "% av varer",
        _ => "% av arbeid"
    };

    public static bool ErProsent(this PrisKodeEnhet e) => e is PrisKodeEnhet.ProsentVarer or PrisKodeEnhet.ProsentArbeid;
}

public class PrisKode
{
    public int Id { get; set; }

    // Samme som varenummeret i Tripletex.
    public required string Kode { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public PrisKodeType Type { get; set; }
    public PrisKodeEnhet Enhet { get; set; }

    // Kroner per enhet, eller prosent når enheten er en prosent.
    public decimal Kostpris { get; set; }
    public decimal Salgspris { get; set; }

    // Koden som timene fra varenes montasjetid legges på.
    public bool ErMontasje { get; set; }
    public bool Aktiv { get; set; } = true;
    public int Rekkefolge { get; set; }
}

// En linje med kode på et tilbud. Kode, navn og priser kopieres fra koden når
// linjen legges til, så endringer i kodelista ikke flytter gamle tilbud.
public class TilbudKodeLinje
{
    public int Id { get; set; }
    public int TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }
    public int? PrisKodeId { get; set; }

    public required string Kode { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public PrisKodeType Type { get; set; }
    public PrisKodeEnhet Enhet { get; set; }
    public decimal Antall { get; set; } = 1;
    public decimal Kostpris { get; set; }
    public decimal Salgspris { get; set; }
    public int Rekkefolge { get; set; }

    // Montasjelinjen følger varenes beregnede montasjetid til antallet endres for hånd.
    public bool FraMontasjetid { get; set; }
}
