namespace PortalItlock.Web.Models;

public class Servicerunde
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public DateTime Dato { get; set; } = DateTime.Now;

    public int? UtfortAvBrukerId { get; set; }
    public Bruker? UtfortAvBruker { get; set; }

    public required string StatusBeskrivelse { get; set; }
    public string? Anbefalinger { get; set; }
    public DateTime? NesteServiceDato { get; set; }
    public bool OppfolgingsTicketOpprettet { get; set; }
    public string? Forside { get; set; }

    // Skiller en planlagt (ikke utført ennå, kun dato satt) fra en vanlig
    // registrert/utført servicerunde. Se "Planlagt servicerunde"-skjemaet i
    // ServiceavtaleDetalj.razor.
    public bool ErPlanlagt { get; set; }

    // Fase 4: runden føres dør for dør (ServicerundeDor). Den pågår til
    // FullfortDato er satt, og er skrivebeskyttet etterpå.
    public bool DorForDor { get; set; }
    public DateTime? FullfortDato { get; set; }

    // Fase 6: kundens signatur på servicerapporten (tegnet på skjermen).
    public byte[]? KundeSignatur { get; set; }
    public string? KundeSignertAv { get; set; }
    public DateTime? KundeSignertDato { get; set; }
    public List<ServicerundeDor> Dorer { get; set; } = [];

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<ServicerundeDel> Deler { get; set; } = [];
    public List<ServicerundeSjekkpunkt> Sjekkpunkter { get; set; } = [];
    public List<ServicerundeMedia> Media { get; set; } = [];
}
