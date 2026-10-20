namespace PortalItlock.Web.Models;

// ---------- Lagerstyring fase 2: innkjøp, ordrebekreftelse, varemottak ----------
//
// Flyt: Utkast → Sendt → Bekreftet (eller Avvik → godkjent → Bekreftet)
// → Delvis mottatt → Mottatt. Varemottak føres som LagerBevegelser av typen
// Mottak med InnkjopsordreId, og innprisen på linjen oppdaterer snittkosten.

public enum InnkjopStatus
{
    Utkast = 0,
    Sendt = 1,
    Bekreftet = 2,
    Avvik = 3,
    DelvisMottatt = 4,
    Mottatt = 5,
    Avsluttet = 6,
    Kansellert = 7
}

public class Innkjopsordre
{
    public int Id { get; set; }

    // Løpenummer som vises som IO-1001, IO-1002 ...
    public int Nummer { get; set; }

    public int? LeverandorId { get; set; }
    public Leverandor? Leverandor { get; set; }

    // Kopi av navn/e-post da ordren ble laget, så ordren er lik selv om
    // leverandørkortet endres senere.
    public required string LeverandorNavn { get; set; }
    public string? LeverandorEpost { get; set; }

    // Lageret varene skal inn på ved mottak.
    public int LagerId { get; set; }
    public Lager? Lager { get; set; }

    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public InnkjopStatus Status { get; set; }

    public DateTime Opprettet { get; set; } = DateTime.Now;
    public string? OpprettetAv { get; set; }
    public DateTime? Sendt { get; set; }
    public string? SendtTil { get; set; }

    public DateTime? OnsketLevering { get; set; }
    public string? VarReferanse { get; set; }
    public string? Notat { get; set; }

    // Ordrebekreftelse fra leverandør.
    public DateTime? BekreftetDato { get; set; }
    public string? LeverandorOrdrenr { get; set; }
    public string? BekreftetAv { get; set; }
    // Navnet på opplastet bekreftelse (selve filen ligger i InnkjopsordreFil).
    public string? ObFilnavn { get; set; }
    public string? AvvikKommentar { get; set; }

    // Fase 5: avvikshistorikk til leverandørrapporten (status blir
    // «Bekreftet» når avviket godkjennes, så dette tas vare på her).
    public bool HaddeAvvik { get; set; }
    public decimal? AvvikPris { get; set; }

    public List<InnkjopsordreLinje> Linjer { get; set; } = [];

    public string Visningsnummer => $"IO-{Nummer}";
}

public class InnkjopsordreLinje
{
    public int Id { get; set; }
    public int InnkjopsordreId { get; set; }
    public Innkjopsordre? Innkjopsordre { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    // Leverandørens varenummer og varenavn da ordren ble laget.
    public string? Varenummer { get; set; }
    public required string Navn { get; set; }
    public string? Enhet { get; set; }

    public int Antall { get; set; }
    public decimal Pris { get; set; }

    // Fra ordrebekreftelsen (null = ikke registrert ennå).
    public int? BekreftetAntall { get; set; }
    public decimal? BekreftetPris { get; set; }
    public DateTime? BekreftetLevering { get; set; }

    public int Mottatt { get; set; }

    public int Rest => Math.Max(0, Antall - Mottatt);
}

// Ordrebekreftelsen som fil (PDF/bilde) - egen tabell så ordrelistene ikke
// laster filinnholdet.
public class InnkjopsordreFil
{
    public int Id { get; set; }
    public int InnkjopsordreId { get; set; }
    public Innkjopsordre? Innkjopsordre { get; set; }
    public required string Filnavn { get; set; }
    public string? ContentType { get; set; }
    // «ordrebekreftelse» eller «pakkseddel» (DokumentTolkService).
    public string Type { get; set; } = "ordrebekreftelse";
    public required byte[] Data { get; set; }
    public DateTime Lastet { get; set; } = DateTime.Now;
}
