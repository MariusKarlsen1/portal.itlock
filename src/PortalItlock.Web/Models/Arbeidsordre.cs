namespace PortalItlock.Web.Models;

public class Arbeidsordre
{
    public int Id { get; set; }
    public required string Tittel { get; set; }
    public string? Beskrivelse { get; set; }

    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    // Kunden arbeidsordren faktureres til - hentes automatisk fra tilbudet
    // (via Tilbud.Prosjekt.Kunde) når arbeidsordren er opprettet fra et
    // tilbud, ellers velges den manuelt. Brukes av
    // TripletexSyncService.PushArbeidsordreTilTripletexAsync, med
    // Prosjekt.Kunde som fallback for eldre arbeidsordre uten denne satt.
    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public int? AnsvarligMontorId { get; set; }
    public Bruker? AnsvarligMontor { get; set; }

    // Flere montører på ordren (i tillegg til ansvarlig montør over).
    public List<ArbeidsordreMontor> Montorer { get; set; } = [];

    /// Ansvarlig eller en av de ekstra montørene. Krever at Montorer er lastet.
    public bool HarMontor(int? brukerId) =>
        brukerId is not null && (AnsvarligMontorId == brukerId || Montorer.Any(m => m.BrukerId == brukerId));

    public int? TilbudId { get; set; }
    public Tilbud? Tilbud { get; set; }

    // Flere tilbud på samme arbeidsordre (i tillegg til TilbudId over).
    public List<ArbeidsordreTilbud> EkstraTilbud { get; set; } = [];

    public ArbeidsordreStatus Status { get; set; } = ArbeidsordreStatus.Ny;
    public ArbeidsordrePrioritet Prioritet { get; set; } = ArbeidsordrePrioritet.Normal;
    public ArbeidsordreType? Type { get; set; }
    public decimal? EstimerteTimer { get; set; }
    public string? Hentehylle { get; set; }
    public DateTime? PlanlagtDato { get; set; }
    public DateTime? PlanlagtSlutt { get; set; }

    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Kontakt for jobben valgt på ordren. Tom: prosjektets kontaktperson. «-»: ingen.
    public string? Kontaktperson { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public string? UtfortArbeid { get; set; }
    public string? Anbefalinger { get; set; }
    public DateTime? FakturertDato { get; set; }
    // Hvem som markerte ordren som fakturert. Settes automatisk ved lagring
    // (ApplicationDbContext) fra den innloggede brukeren.
    public int? FakturertAvBrukerId { get; set; }
    public string? FakturertAvNavn { get; set; }
    public string? KundeInfoForJobb { get; set; }
    public DateTime? DokumentasjonEpostSendtDato { get; set; }

    // Varesalg rett fra et godkjent tilbud («Fakturer direkte»): ingen montør
    // eller timer, varene er trukket fra lager og ordren er ferdigmeldt med en gang.
    public bool ErDirektesalg { get; set; }

    public int? FullfortAvBrukerId { get; set; }
    public Bruker? FullfortAvBruker { get; set; }
    public DateTime? FullfortDato { get; set; }

    // Montøren har ferdigmeldt jobben fra mobilen. Ordren står da «Til kontroll»:
    // kontoret sjekker timer, varer og notater, og ferdigmelder selv. Først da
    // går den til Tripletex.
    public DateTime? MontorFerdigmeldtDato { get; set; }
    public int? MontorFerdigmeldtAvBrukerId { get; set; }
    public string? MontorFerdigmeldtAvNavn { get; set; }

    public bool ErTilKontroll => MontorFerdigmeldtDato is not null
        && Status is not (ArbeidsordreStatus.Ferdig or ArbeidsordreStatus.Fakturert);

    // Satt når ordren er sendt til Tripletex som en ordre klar til fakturering
    // (se TripletexSyncService.PushArbeidsordreTilTripletexAsync, kalt fra
    // Ferdigmeld() i ArbeidsordreSkjema.razor) - hindrer at samme arbeidsordre
    // sendes dit flere ganger.
    public int? TripletexOrdreId { get; set; }
    public string? TripletexOrdreNummer { get; set; }
    public DateTime? TripletexOrdreSendtDato { get; set; }
    public string? TripletexOrdreFeil { get; set; }

    // Kun i bruk når arbeidsordren ikke er koblet til et tilbud - da har man ingen
    // Tilbud-linjer å hente montasjepris/DG fra, så det settes manuelt her.
    public decimal? Timepris { get; set; }
    public decimal? KostprisTime { get; set; }

    public List<Timeregistrering> Timeregistreringer { get; set; } = [];
    public List<ArbeidsordreSjekkpunkt> Sjekkpunkter { get; set; } = [];
    public List<ArbeidsordreMedia> Media { get; set; } = [];
    public List<ArbeidsordreVare> Varer { get; set; } = [];
    public List<ArbeidsordreKodeLinje> KodeLinjer { get; set; } = [];
}
