namespace PortalItlock.Web.Models;

public class Kunngjoring
{
    public int Id { get; set; }
    public required string Tittel { get; set; }
    public required string Innhold { get; set; }
    public KunngjoringKategori Kategori { get; set; } = KunngjoringKategori.Info;
    public bool Festet { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    // Valgfrie felter for arrangement-type kunngjøringer (f.eks. Firmatur) -
    // vises kun i detaljvisningen når satt, se KunngjoringDetalj.razor.
    public DateTime? EventDato { get; set; }
    public string? Sted { get; set; }
    public string? Detaljer { get; set; }
    public string? InfoTekst { get; set; }

    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }

    public byte[]? BildeData { get; set; }
    public string? BildeContentType { get; set; }
    public string? BildeFilnavn { get; set; }

    // Fellesskap og innleggstype (oktober 2026, se Fellesskap.cs / InnleggType.cs).
    public int? FellesskapId { get; set; }
    public Fellesskap? Fellesskap { get; set; }
    public InnleggType Type { get; set; } = InnleggType.Kunngjoring;

    // Fargebakgrunn for korte innlegg (InnleggBakgrunn) - bilde bruker BildeData.
    public string? Bakgrunn { get; set; }
    public bool KreverBekreftelse { get; set; }

    public int? RosTilBrukerId { get; set; }
    public Bruker? RosTilBruker { get; set; }
    public string? RosMerke { get; set; }

    // Spørsmål: kommentaren som er merket som beste svar (= løst).
    public int? BesteSvarKommentarId { get; set; }

    public List<KunngjoringLike> Likes { get; set; } = [];
    public List<KunngjoringBekreftelse> Bekreftelser { get; set; } = [];
    public List<KunngjoringVisning> Visninger { get; set; } = [];
    public List<AvstemningValg> Valg { get; set; } = [];
    public List<KunngjoringKommentar> Kommentarer { get; set; } = [];
    public List<KunngjoringPamelding> Pameldinger { get; set; } = [];
}
