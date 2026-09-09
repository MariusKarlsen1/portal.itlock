namespace PortalItlock.Web.Models;

public class Ticket
{
    public int Id { get; set; }
    public required string Tittel { get; set; }
    public string? Beskrivelse { get; set; }

    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public string? Lokasjon { get; set; }
    public string? Kontaktperson { get; set; }

    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int? ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public string? Board { get; set; }
    public string? Type { get; set; }
    public string? Subtype { get; set; }
    public string? ProduktAnlegg { get; set; }

    public TicketPrioritet Prioritet { get; set; } = TicketPrioritet.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.Ny;
    public DateTime? SlaFrist { get; set; } = TicketSlaHelper.BeregnFrist(TicketPrioritet.Normal, DateTime.Now);
    public bool EskaleringsVarselSendt { get; set; }
    public string? Sluttoppsummering { get; set; }

    public int? AnsvarligBrukerId { get; set; }
    public Bruker? AnsvarligBruker { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public DateTime? LukketDato { get; set; }

    public int? GodkjentAvBrukerId { get; set; }
    public Bruker? GodkjentAvBruker { get; set; }

    public int? ForesporselId { get; set; }
    public Foresporsel? Foresporsel { get; set; }

    public int? ServicehenvendelseId { get; set; }
    public Servicehenvendelse? Servicehenvendelse { get; set; }

    public int? DriftsmeldingId { get; set; }
    public Driftsmelding? Driftsmelding { get; set; }

    public int? DorId { get; set; }
    public Dor? Dor { get; set; }
    public bool ErReklamasjon { get; set; }

    public List<TicketHendelse> Hendelser { get; set; } = [];
    public List<TicketMedia> Media { get; set; } = [];
}
