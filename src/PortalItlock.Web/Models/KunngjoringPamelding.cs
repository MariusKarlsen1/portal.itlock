namespace PortalItlock.Web.Models;

// Svar på et arrangement i en kunngjøring (Kunngjoring.EventDato satt).
public class KunngjoringPamelding
{
    public int Id { get; set; }
    public int KunngjoringId { get; set; }
    public Kunngjoring? Kunngjoring { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public PameldingSvar Svar { get; set; } = PameldingSvar.Kommer;
    public DateTime OppdatertDato { get; set; } = DateTime.Now;
}

public enum PameldingSvar { Kommer, Kanskje, KanIkke }
