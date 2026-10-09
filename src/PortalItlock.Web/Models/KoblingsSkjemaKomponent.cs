namespace PortalItlock.Web.Models;

// Ett kompatibelt produkt/komponent for et koblingsskjema - vises både som en
// kompakt liste under Informasjon-fanen og som en redigerbar tabell under
// Komponenter-fanen (se KoblingKategori.razor).
public class KoblingsSkjemaKomponent
{
    public int Id { get; set; }
    public int KoblingsSkjemaId { get; set; }
    public KoblingsSkjema? KoblingsSkjema { get; set; }

    public required string Navn { get; set; }
    public string? Type { get; set; }
    public string Ikon { get; set; } = "box";
    public int Rekkefolge { get; set; }
}
