namespace PortalItlock.Web.Models;

// ---------- Lagerstyring (tilleggsmodul, fase 1) ----------
//
// Beholdningen regnes alltid ut fra LagerBevegelser - ingen skriver over et
// antall direkte. Feil rettes med en ny bevegelse (korrigering), så hele
// historikken er med. Component.Lagerantall holdes oppdatert som summen over
// alle lagre (se LagerService), slik at eksisterende kode som leser det
// (varsel om lav beholdning, komponentregisteret) fortsatt virker.

public enum LagerType
{
    Hovedlager = 0,
    Bil = 1,
    Prosjektlager = 2
}

public class Lager
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public LagerType Type { get; set; }

    // Montøren som har bilen, eller ansvarlig for prosjektlageret.
    public int? AnsvarligBrukerId { get; set; }
    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public bool Aktiv { get; set; } = true;
    public DateTime Opprettet { get; set; } = DateTime.Now;
}

public enum LagerBevegelseType
{
    Inngaende = 0,      // startbeholdning da lagermodulen ble tatt i bruk
    Tilgang = 1,        // manuelt lagt til (f.eks. funnet, kjøpt kontant)
    Uttak = 2,          // manuelt tatt ut (f.eks. ødelagt, svinn)
    FlyttUt = 3,        // flyttet til et annet lager (motpart: FlyttInn)
    FlyttInn = 4,
    Telling = 5,        // avvik fra varetelling
    Korrigering = 6,    // retting av feil registrering
    Mottak = 7,         // varemottak fra leverandør (fase 2)
    Forbruk = 8,        // brukt på prosjekt/arbeidsordre (fase 4)
    Retur = 9           // retur fra montør/prosjekt (fase 4)
}

public class LagerBevegelse
{
    public int Id { get; set; }
    public DateTime Tidspunkt { get; set; } = DateTime.Now;

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int LagerId { get; set; }
    public Lager? Lager { get; set; }

    public LagerBevegelseType Type { get; set; }

    // Positivt inn, negativt ut.
    public int Antall { get; set; }

    // Kost per stk på tidspunktet (snittkost ved uttak, innpris ved tilgang).
    public decimal KostPerStk { get; set; }

    // Samme id på FlyttUt og FlyttInn, så de to radene hører sammen.
    public Guid? FlyttId { get; set; }

    public int? ProsjektId { get; set; }
    public int? ArbeidsordreId { get; set; }
    public int? VaretellingId { get; set; }

    public int? BrukerId { get; set; }
    public string? BrukerNavn { get; set; }
    public string? Notat { get; set; }
}

// Hylleplass for en vare på et bestemt lager (A-02, Reol 3 hylle 1 ...).
public class VareLagerplass
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public Component? Component { get; set; }
    public int LagerId { get; set; }
    public Lager? Lager { get; set; }
    public string? Hylle { get; set; }
}

public enum VaretellingStatus
{
    Pagar = 0,
    Godkjent = 1,
    Avbrutt = 2
}

public class Varetelling
{
    public int Id { get; set; }
    public int LagerId { get; set; }
    public Lager? Lager { get; set; }
    public VaretellingStatus Status { get; set; }
    public DateTime Startet { get; set; } = DateTime.Now;
    public DateTime? Godkjent { get; set; }
    public string? StartetAv { get; set; }
    public string? GodkjentAv { get; set; }
    public string? Notat { get; set; }
    public List<VaretellingLinje> Linjer { get; set; } = [];
}

public class VaretellingLinje
{
    public int Id { get; set; }
    public int VaretellingId { get; set; }
    public Varetelling? Varetelling { get; set; }
    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    // Beholdningen da tellingen startet. Avviket som føres er Talt minus
    // Registrert, så bevegelser som skjer mens man teller ikke telles dobbelt.
    public int Registrert { get; set; }
    public int? Talt { get; set; }
}
