namespace PortalItlock.Web.Models;

// Én sikkerhetskopi av en organisasjons database (med bilder og vedlegg,
// som ligger i samme fil). ErPlattform = plattformdatabasen.
// Selve filen er komprimert og kryptert, og ligger i lagringen Sti peker til.
public class TenantBackup
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Plattformdatabasen (organisasjoner, lisenser, fakturaer) i stedet for en organisasjon.
    public bool ErPlattform { get; set; }
    // Navnet på organisasjonen da kopien ble tatt, så den kan kjennes igjen
    // også om organisasjonen senere slettes.
    public string? Navn { get; set; }

    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public BackupType Type { get; set; }
    public long Storrelse { get; set; }
    public long DatabaseStorrelse { get; set; }
    public BackupLagringType Lagring { get; set; }
    public required string Sti { get; set; }
    public string? Sha256 { get; set; }

    // Kopien er åpnet og kontrollert (PRAGMA integrity_check) etter at den ble tatt.
    public bool Kontrollert { get; set; }
    public string? Feil { get; set; }
    public string? Av { get; set; }
}

public enum BackupType
{
    Daglig,
    Ukentlig,
    Manedlig,
    Manuell,
    ForGjenoppretting
}

public enum BackupLagringType
{
    Lokal,
    Azure
}

public static class BackupTypeExtensions
{
    public static string Visningsnavn(this BackupType type) => type switch
    {
        BackupType.Ukentlig => "Ukentlig",
        BackupType.Manedlig => "Månedlig",
        BackupType.Manuell => "Tatt manuelt",
        BackupType.ForGjenoppretting => "Før gjenoppretting",
        _ => "Daglig"
    };
}
