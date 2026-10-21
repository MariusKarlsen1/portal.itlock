namespace PortalItlock.Web.Models;

// ---------- Prisliste, abonnement og fakturaer for organisasjonene ----------
// Alt ligger i PlatformDbContext. Prislisten er felles for alle; hver
// organisasjon kan ha egen avtalepris. Fakturaene lages fra abonnementet og
// sendes som ordre til Full Kontroll sin Tripletex.

public enum PrisType
{
    Fast,
    Lisens,
    Modul,
    Engang
}

public static class PrisTypeExtensions
{
    public static string Visningsnavn(this PrisType type) => type switch
    {
        PrisType.Fast => "Fast",
        PrisType.Lisens => "Lisens",
        PrisType.Modul => "Tilleggsmodul",
        PrisType.Engang => "Engang",
        _ => type.ToString()
    };

    public static string Enhet(this PrisType type) => type switch
    {
        PrisType.Lisens => "per stk. per måned",
        PrisType.Engang => "én gang",
        _ => "per måned"
    };
}

// Én rad i prislisten. Nokkel kobler prisen til det den gjelder:
// "grunnpakke", "etablering", "lisens:Brukere", "lisens:KundeTilgang",
// "modul:lager".
public class PrisProdukt
{
    public int Id { get; set; }
    public required string Nokkel { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public PrisType Type { get; set; }
    public decimal Kr { get; set; }
    // Selges i pakker på så mange (f.eks. 10 driftslisenser); prisen gjelder per pakke.
    public int? PakkeStorrelse { get; set; }
    public int Rekkefolge { get; set; }

    public const string Grunnpakke = "grunnpakke";
    public const string Etablering = "etablering";
    public static string ForLisens(LisensType type) => $"lisens:{type}";
    public static string ForModul(string modul) => $"modul:{modul}";
}

// Egen pris for én organisasjon. Mangler raden, gjelder listeprisen.
public class TenantAvtalepris
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public required string Nokkel { get; set; }
    public decimal Kr { get; set; }
}

public enum FakturaIntervall
{
    Maned = 1,
    Kvartal = 3,
    Ar = 12
}

public static class FakturaIntervallExtensions
{
    public static string Visningsnavn(this FakturaIntervall i) => i switch
    {
        FakturaIntervall.Kvartal => "Kvartalsvis, forskuddsvis",
        FakturaIntervall.Ar => "Årlig, forskuddsvis",
        _ => "Månedlig, forskuddsvis"
    };
}

public class TenantAbonnement
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public DateTime Startdato { get; set; } = DateTime.Today;
    public DateTime? BindingTil { get; set; }
    public bool Grunnpakke { get; set; } = true;
    // Pakken (PrisPakke.Nokkel), f.eks. "komplett". Null = listeprisene.
    public string? Pakke { get; set; }
    public FakturaIntervall Intervall { get; set; } = FakturaIntervall.Maned;
    public int BetalingsfristDager { get; set; } = 14;
    public string? FakturaEpost { get; set; }

    // Første dag i perioden neste faktura skal gjelde.
    public DateTime NesteFakturaFra { get; set; } = DateTime.Today;
    public bool EtableringFakturert { get; set; }
    // Etableringsgebyr for denne kunden; null = listeprisen.
    public decimal? Etableringsgebyr { get; set; }

    // Prøveperiode: organisasjonen er aktiv, men faktureres ikke før etter denne datoen.
    public DateTime? ProveperiodeTil { get; set; }

    // Avtale og databehandleravtale (GDPR), registrert når kunden har signert.
    public DateTime? AvtaleSignert { get; set; }
    public DateTime? DatabehandleravtaleSignert { get; set; }

    public DateTime? OppsagtDato { get; set; }
    public DateTime? PauseFra { get; set; }

    // Kunden i Full Kontroll sin Tripletex.
    public int? TripletexKundeId { get; set; }
}

// Logg over endringer i antall, så økninger midt i en periode kan
// faktureres forholdsmessig på neste faktura.
public class TenantAntallEndring
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public required string Nokkel { get; set; }
    public int FraAntall { get; set; }
    public int TilAntall { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
    public bool Fakturert { get; set; }
}

public enum PlattformFakturaStatus
{
    Utkast,
    SendtTilTripletex,
    Fakturert,
    Betalt,
    Kreditert
}

public class PlattformFaktura
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public DateTime PeriodeFra { get; set; }
    public DateTime PeriodeTil { get; set; }
    public DateTime Opprettet { get; set; } = DateTime.Now;
    public PlattformFakturaStatus Status { get; set; }
    public decimal BelopEksMva { get; set; }

    public int? TripletexOrdreId { get; set; }
    public string? TripletexOrdreNummer { get; set; }
    public int? TripletexFakturaNummer { get; set; }
    public DateTime? Fakturadato { get; set; }
    public DateTime? Forfall { get; set; }
    public decimal? Utestaende { get; set; }
    public DateTime? BetaltDato { get; set; }
    public string? Feil { get; set; }

    public List<PlattformFakturaLinje> Linjer { get; set; } = [];

    public bool ErForfalt => Status == PlattformFakturaStatus.Fakturert && Forfall is { } f && f.Date < DateTime.Today;
}

public class PlattformFakturaLinje
{
    public int Id { get; set; }
    public int FakturaId { get; set; }
    public PlattformFaktura? Faktura { get; set; }
    public required string Tekst { get; set; }
    public decimal Antall { get; set; }
    public decimal Pris { get; set; }
    public decimal Belop => Math.Round(Antall * Pris, 2);
    public int Rekkefolge { get; set; }
}

// Notater fra samtaler med kunden, med valgfri dato for oppfølging.
public class TenantNotat
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
    public string Type { get; set; } = "Telefon";
    public required string Tekst { get; set; }
    public DateTime? Oppfolging { get; set; }
    public bool Utfort { get; set; }
    public string? Av { get; set; }
}

// Logg over det som skjer med organisasjonen (abonnement, status, fakturaer).
public class TenantHendelse
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public required string Tekst { get; set; }
    public string Ikon { get; set; } = "edit";
    public string? Av { get; set; }
}
