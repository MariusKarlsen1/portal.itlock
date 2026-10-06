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

    public List<KunngjoringLike> Likes { get; set; } = [];
    public List<KunngjoringKommentar> Kommentarer { get; set; } = [];
}
