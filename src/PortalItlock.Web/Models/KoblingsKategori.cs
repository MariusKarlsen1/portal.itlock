namespace PortalItlock.Web.Models;

public class KoblingsKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public string Ikon { get; set; } = "krets-brikke";
    public string Farge { get; set; } = "oransje";
    public int Rekkefolge { get; set; }

    // Vises som kolonnene "Kategori" og "Produsent" i systemtabellen på
    // /guide/kobling. Begge er valgfrie: uten verdi står det "Andre" og "-",
    // slik at systemer som fantes før kolonnene kom fortsatt ser riktige ut.
    public string? Gruppe { get; set; }
    public string? Produsent { get; set; }

    public List<KoblingsSkjema> Skjemaer { get; set; } = [];
}
