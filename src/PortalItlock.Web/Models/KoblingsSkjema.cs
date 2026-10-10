namespace PortalItlock.Web.Models;

public class KoblingsSkjema
{
    public int Id { get; set; }
    public int KategoriId { get; set; }
    public KoblingsKategori? Kategori { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public string? Versjon { get; set; }
    public string? Notater { get; set; }

    // Visningsvalg for selve tegneflaten, satt i "Skjema-innstillinger" i
    // editoren. Lagres pr. skjema slik at de overlever at man lukker siden.
    public string Stil { get; set; } = "Standard";
    public bool VisRutenett { get; set; } = true;
    public bool SnapTilRutenett { get; set; } = true;
    public bool VisKomponentnavn { get; set; } = true;

    // Valgfri kobling til et prosjekt - skjemaet vises da som vedlegg på prosjektet.
    public int? ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    // Valgfri kobling til en dør - skjemaet vises da som prinsippskisse på dørsiden.
    public int? DorId { get; set; }
    public Dor? Dor { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
    public DateTime? OppdatertDato { get; set; }

    // Satt hver gang noen faktisk åpner/ser på skjemaet (ikke bare redigerer
    // det) - styrer "Sist brukte"-lista på /guide/kobling, se KoblingKategori.razor
    // (VelgSkjema) og KoblingsSkjemaVisning.razor.
    public DateTime? SistApnet { get; set; }

    public List<KoblingsSymbol> Symboler { get; set; } = [];
    public List<KoblingsStrek> Streker { get; set; } = [];
    public List<KoblingsSkjemaKomponent> Komponenter { get; set; } = [];
}
