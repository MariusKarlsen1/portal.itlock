namespace PortalItlock.Web.Models;

// ---------- Prosjektøkonomi og etterkalkyle ----------
//
// Fanen «Økonomi» i prosjektet sammenligner det som ble tilbudt (godkjente
// tilbud og endringsmeldinger) med det prosjektet faktisk har kostet (timer
// og varer), og regner ut en prognose fra fremdriften. Når prosjektet
// settes til Overlevert, havner det i modulen «Etterkalkyle» for
// gjennomgang. Se Services/ProsjektOkonomiService.cs.

// Én rad per organisasjon - redigeres under Etterkalkyle → Innstillinger.
public class OkonomiInnstillinger
{
    public int Id { get; set; }

    // Intern kostpris per time (lønn + sosiale kostnader). Brukes når
    // arbeidsordren ikke har egen kostpris; mangler den også, brukes
    // tilbudets montasje-innpris.
    public decimal? InternTimekost { get; set; }

    // Gult varsel når prognosen er så mange prosent over budsjettet ...
    public int GulProsentOverBudsjett { get; set; } = 10;

    // ... eller når så mye av timene er brukt før fremdriften har nådd
    // GulFremdriftProsent.
    public int GulTimerBruktProsent { get; set; } = 80;
    public int GulFremdriftProsent { get; set; } = 60;
}

// Budsjettet fra et godkjent tilbud, låst første gang økonomien regnes ut
// etter godkjenning - så det ikke flytter seg om tilbudet redigeres senere.
public class TilbudBudsjett
{
    public int Id { get; set; }
    public int TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }

    public decimal Salgssum { get; set; }
    public decimal Varekost { get; set; }
    public decimal Timer { get; set; }
    public decimal Timesats { get; set; }

    // Kjøring, småmateriell og tillegg fra kodelinjene (alt som ikke er
    // timer). Følges som egen post, anslått etter fremdriften.
    public decimal AndreKost { get; set; }

    // Delen av AndreKost som er prosentlinjer (småmateriell, rigg og drift).
    // Den registreres ikke underveis og anslås alltid etter fremdriften.
    public decimal AndreProsentKost { get; set; }
    public DateTime Laast { get; set; } = DateTime.Now;
}

public enum EtterkalkyleArsak
{
    IngenAvvik = 0,
    Ekstraarbeid = 1,
    FeilITilbud = 2,
    Ventetid = 3,
    Varekostnad = 4,
    Annet = 5
}

// Signert gjennomgang av et overlevert prosjekt. Tallene lagres slik de var
// da prosjektet ble kontrollert.
public class ProsjektEtterkalkyle
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public DateTime Kontrollert { get; set; } = DateTime.Now;
    public string? KontrollertAv { get; set; }
    public EtterkalkyleArsak Arsak { get; set; }
    public string? Kommentar { get; set; }

    // Spørsmålene fra etterkalkylen i økonomiarket.
    public string? GikkBra { get; set; }
    public string? GikkDarlig { get; set; }
    public string? Forbedre { get; set; }
    public string? ForsinkelseEgne { get; set; }
    public string? ForsinkelseAndre { get; set; }

    // Gjennomgangen er lagret, men ikke signert. Prosjektet står fortsatt
    // under Til gjennomgang.
    public bool ErUtkast { get; set; }

    public decimal Inntekt { get; set; }
    public decimal BudsjettTimer { get; set; }
    public decimal FaktiskTimer { get; set; }
    public decimal BudsjettVarer { get; set; }
    public decimal FaktiskVarer { get; set; }
    public decimal BudsjettKost { get; set; }
    public decimal FaktiskKost { get; set; }
    public decimal? BudsjettDekningsgrad { get; set; }
    public decimal? FaktiskDekningsgrad { get; set; }
}

public static class EtterkalkyleArsakExtensions
{
    public static string Visningsnavn(this EtterkalkyleArsak a) => a switch
    {
        EtterkalkyleArsak.IngenAvvik => "Ingen vesentlige avvik",
        EtterkalkyleArsak.Ekstraarbeid => "Ekstraarbeid",
        EtterkalkyleArsak.FeilITilbud => "Feil eller mangler i tilbudet",
        EtterkalkyleArsak.Ventetid => "Ventetid eller forsinkelser",
        EtterkalkyleArsak.Varekostnad => "Høyere varekostnad",
        _ => "Annet"
    };
}
