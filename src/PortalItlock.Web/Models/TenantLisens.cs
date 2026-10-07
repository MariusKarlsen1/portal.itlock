namespace PortalItlock.Web.Models;

// Antall lisenser plattformeier har aktivert for en organisasjon - sjekkes
// mot faktisk forbruk (telling i organisasjonens egen database) før de får
// lov til å opprette flere brukere/kundetilganger i sin portal. Ligger i
// PlatformDbContext, ikke i organisasjonens egen ApplicationDbContext.
public class TenantLisens
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public LisensType Type { get; set; }
    public int AntallTildelt { get; set; }
    public DateTime SistEndret { get; set; } = DateTime.Now;
}
