namespace PortalItlock.Web.Models;

public class NedlastningsKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public string Farge { get; set; } = "brun";
    public int Rekkefolge { get; set; }

    // Vises som kolonnene "Kategori" og "Produsent" i systemtabellen på
    // /guide/oppsett-adgangskontroll. Begge er valgfrie: uten verdi står det
    // "Andre" og "-".
    public string? Gruppe { get; set; }
    public string? Produsent { get; set; }

    public List<NedlastningsFil> Filer { get; set; } = [];
}
