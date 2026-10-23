namespace PortalItlock.Web.Models;

// ---------- Oppfølging av pågående prosjekter ----------
//
// Det som var arket «Kalkulering og økonomi» (sjekkliste, budsjett, faktura
// og timekost), lagret per prosjekt. Tallene som kan regnes ut (timer, varer,
// tilbud, endringer) lagres ikke her; bare det brukeren fyller inn.
// Se Services/ProsjektOppfolgingService.cs og Prosjektokonomi.razor.

public enum AkontoStatus
{
    Planlagt = 0,
    Sendt = 1,
    Betalt = 2
}

// Én linje i faktureringsplanen. Beløpet er Prosent av kontraktsummen, eller
// et fast Belop når Prosent er tom. Akontoen blir «klar» når fremdriften har
// nådd FremdriftProsent, eller når PlanlagtDato er passert.
public class ProsjektAkonto
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int Nr { get; set; }
    public required string Milepael { get; set; }
    public decimal? Prosent { get; set; }
    public decimal? Belop { get; set; }
    public int? FremdriftProsent { get; set; }
    public DateTime? PlanlagtDato { get; set; }

    public AkontoStatus Status { get; set; }
    public DateTime? SendtDato { get; set; }
    public decimal? SendtBelop { get; set; }
    public int? TripletexOrdreId { get; set; }
    public string? TripletexOrdreNummer { get; set; }
    public string? TripletexFeil { get; set; }
    public DateTime? BetaltDato { get; set; }
}

public enum BudsjettPost
{
    Varer = 0,
    Timer = 1,
    Uforutsett = 2
}

// Justering av budsjettet underveis (kolonnen «Justeringer underveis» i
// arket). Én rad per post og prosjekt.
public class ProsjektBudsjettJustering
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }
    public BudsjettPost Post { get; set; }
    public decimal Belop { get; set; }
}

public enum PlanOppgaveStatus
{
    IkkeStartet = 0,
    Pabegynt = 1,
    Utfordring = 2,
    Avsluttet = 3,
    IkkeAktuelt = 4
}

public static class PlanOppgaveStatusExtensions
{
    public static string Visningsnavn(this PlanOppgaveStatus s) => s switch
    {
        PlanOppgaveStatus.Pabegynt => "Påbegynt",
        PlanOppgaveStatus.Utfordring => "Utfordring",
        PlanOppgaveStatus.Avsluttet => "Avsluttet",
        PlanOppgaveStatus.IkkeAktuelt => "Ikke aktuelt",
        _ => "Ikke startet"
    };

    // Som i arket: avsluttet og ikke aktuelt teller som ferdig.
    public static bool ErFerdig(this PlanOppgaveStatus s) => s is PlanOppgaveStatus.Avsluttet or PlanOppgaveStatus.IkkeAktuelt;
}

// En aktivitet i fremdriftsplanen (for eksempel «Kalkulasjon»). Datoene
// regnes fra fasen: DelFra og DelTil er andeler av fasens lengde, så
// aktiviteten flytter seg når fristene på prosjektet flyttes.
public class ProsjektplanAktivitet
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int Fase { get; set; }
    public required string Navn { get; set; }
    public string? Rolle { get; set; }
    public decimal DelFra { get; set; }
    public decimal DelTil { get; set; } = 1;
    public int Rekkefolge { get; set; }

    public List<ProsjektplanOppgave> Oppgaver { get; set; } = [];
}

public class ProsjektplanOppgave
{
    public int Id { get; set; }
    public int AktivitetId { get; set; }
    public ProsjektplanAktivitet? Aktivitet { get; set; }

    public required string Tekst { get; set; }
    public string? Rolle { get; set; }
    public PlanOppgaveStatus Status { get; set; }
    public int Rekkefolge { get; set; }
    public DateTime? Endret { get; set; }
    public string? EndretAv { get; set; }
}

// Rolle → person på prosjektet (rolletabellen øverst i sjekklisten).
public class ProsjektRolle
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }
    public required string Rolle { get; set; }
    public int? BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
}

// Timekost per ansatt, som arket «Timekost»: (årslønn × (1 + sosiale
// kostnader) + overhead) / fakturerbare timer.
public class AnsattTimekost
{
    public int Id { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public decimal Arslonn { get; set; }
    public decimal SosialeProsent { get; set; } = 35;
    public decimal Overhead { get; set; }
    public decimal FakturerbareTimer { get; set; } = 1600;

    public decimal Timekost => FakturerbareTimer > 0 ? Math.Round((Arslonn * (1 + SosialeProsent / 100m) + Overhead) / FakturerbareTimer, 2) : 0;
}
