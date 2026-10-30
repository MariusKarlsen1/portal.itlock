namespace PortalItlock.Web.Models;

// Tilleggsmoduler en organisasjon kan kjøpe. Plattformeier slår dem på per
// organisasjon på /plattform/organisasjoner/{id}. Lagres i PlatformDbContext
// (ikke i organisasjonens egen database), på samme måte som TenantLisens.
public class TenantModul
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Nøkkelen fra TilleggsModuler (f.eks. "lager").
    public required string Modul { get; set; }
    public bool Aktiv { get; set; }
    public DateTime? AktivertDato { get; set; }
    public DateTime SistEndret { get; set; } = DateTime.Now;
}

public static class TilleggsModuler
{
    public const string Lager = "lager";
    public const string Ce = "ce";
    public const string Timer = "timer";
    public const string Okonomi = "prosjektokonomi";
    public const string Kassa = "kassa";

    public sealed record Info(string Nokkel, string Navn, string Beskrivelse);

    public static readonly Info[] Alle =
    [
        new(Timer, "Timer og fravær", "Timeregistrering, fravær, kalender og planlegging, og godkjenning av timer."),
        new(Okonomi, "Prosjektøkonomi", "Budsjett, etterkalkyle og lønnsomhet per prosjekt, med varsel før budsjettet er brukt opp."),
        new(Lager, "Lagerstyring", "Lagre (hovedlager, biler og prosjektlager), beholdning, lagerbevegelser, lagerverdi og varetelling."),
        new(Kassa, "Kassa", "Salg i butikk med kontantfaktura, kort/kontant, dagsoppgjør, månedsoppgjør og bankavstemming."),
        new(Ce, "CE og risikovurdering", "CE-sertifisering av automatiske dører, risikovurdering, samsvarserklæring og varsel før CE går ut."),
    ];
}
