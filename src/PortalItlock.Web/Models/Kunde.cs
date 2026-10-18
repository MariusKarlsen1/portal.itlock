namespace PortalItlock.Web.Models;

public class Kunde
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? OrgNr { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Notater { get; set; }
    public string? TripletexKundenummer { get; set; }

    // Selve bildet ligger i samme tabell, men er skilt ut i KundeBilde slik at
    // det IKKE lastes hver gang en kunde hentes - Include(a => a.Kunde) på f.eks.
    // arbeidsordrelisten hentet ellers kundebildet én gang per rad. Der bildet
    // faktisk trengs: db.LastBildeAsync(kunde) (eller Include(k => k.Bilde) på
    // ett enkelt oppslag), og alltid før BildeData endres.
    public KundeBilde? Bilde { get; set; }

    private KundeBilde SikreBilde() => Bilde ??= new KundeBilde { Id = Id };

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public byte[]? BildeData { get => Bilde?.BildeData; set => SikreBilde().BildeData = value; }
    public string? BildeContentType { get; set; }
    public string? BildeFilnavn { get; set; }
    public DateTime? NesteOppfolgingsDato { get; set; }
    public decimal? FastPaslagProsent { get; set; }

    // Brukes til å sortere/summere salg pr. kundetype i Kunderapporten - se
    // RapportKunde.razor.
    public KundeType? Type { get; set; }
    public KundeStatus Status { get; set; } = KundeStatus.Aktiv;

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Prosjekt> Prosjekter { get; set; } = [];
    public List<KundeOppfolgingNotat> OppfolgingNotater { get; set; } = [];
    public List<KundeLisens> Lisenser { get; set; } = [];
    public List<KundeKontaktperson> Kontaktpersoner { get; set; } = [];
    public List<KundeDokument> Dokumenter { get; set; } = [];
    public List<KundeHendelse> Hendelser { get; set; } = [];
}
