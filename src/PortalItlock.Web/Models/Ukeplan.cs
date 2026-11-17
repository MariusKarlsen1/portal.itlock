namespace PortalItlock.Web.Models;

// ---------- Ressursplan (timer per uke fremover) ----------
//
// Modulen «Ressursplan» for prosjektledere og admin: én rad per prosjekt eller
// manuell post, med timer per uke, mot en manuell kapasitet per uke. Heter
// Ukeplan* i koden fordi Ressursplan allerede er kalenderboksene i Kalender.

public enum UkeplanKategori
{
    Store = 0,
    Mindre = 1,
    Service = 2,
    Potensiell = 3
}

public class UkeplanRad
{
    public int Id { get; set; }

    // Satt når raden hører til et prosjekt (hentes inn automatisk). Null = lagt inn manuelt.
    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    // Navn på manuelle rader (prosjektrader bruker prosjektets navn).
    public string? Navn { get; set; }

    public UkeplanKategori Kategori { get; set; } = UkeplanKategori.Mindre;

    public int? AnsvarligId { get; set; }
    public Bruker? Ansvarlig { get; set; }

    // For potensielle prosjekter (tilbud sendt): prosent som telles med i summen.
    public int Sannsynlighet { get; set; } = 50;

    // Tatt ut av planen (av brukeren, eller fordi tilbudet ble avslått). Raden
    // beholdes så den ikke hentes inn igjen, og så den kan legges tilbake.
    public bool Skjult { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public List<UkeplanTimer> Timer { get; set; } = [];
}

public class UkeplanTimer
{
    public int Id { get; set; }
    public int RadId { get; set; }
    public UkeplanRad? Rad { get; set; }
    public int Ar { get; set; }
    public int Uke { get; set; }
    public decimal Timer { get; set; }
}

// Kapasitet i timer per uke, lagt inn manuelt. Ar = 0 og Uke = 0 er standarden
// som gjelder alle uker uten egen verdi.
public class UkeplanKapasitet
{
    public int Id { get; set; }
    public int Ar { get; set; }
    public int Uke { get; set; }
    public decimal Timer { get; set; }
}

// Endringer fra prosjektene som vises under «Endringer»: nye prosjekter som
// skal planlegges, tilbud som er avslått (fjernet) og tilbud som er godkjent.
public enum UkeplanEndringType
{
    Ny = 0,
    Avslatt = 1,
    Godkjent = 2
}

public class UkeplanEndring
{
    public int Id { get; set; }
    public UkeplanEndringType Type { get; set; }
    public int? RadId { get; set; }
    public UkeplanRad? Rad { get; set; }
    public required string Tekst { get; set; }
    public string? Under { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
    public bool Kvittert { get; set; }
}
