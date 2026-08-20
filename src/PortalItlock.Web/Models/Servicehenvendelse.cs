namespace PortalItlock.Web.Models;

public class Servicehenvendelse
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public required string Dortype { get; set; }
    public string? Beskrivelse { get; set; }
    public string? Adresse { get; set; }
    public string? OnsketTidspunkt { get; set; }

    public ServicehenvendelseStatus Status { get; set; } = ServicehenvendelseStatus.Ny;
    public string? SvarFraItlock { get; set; }
    public DateTime? EpostSendtDato { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<ServicehenvendelseBilde> Bilder { get; set; } = [];
}
