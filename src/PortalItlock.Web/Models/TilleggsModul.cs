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

    public sealed record Info(string Nokkel, string Navn, string Beskrivelse);

    public static readonly Info[] Alle =
    [
        new(Lager, "Lagerstyring", "Lagre (hovedlager, biler og prosjektlager), beholdning, lagerbevegelser, lagerverdi og varetelling."),
    ];
}
