namespace PortalItlock.Web.Models;

public enum KoblingsKategori { ARX, Salto, Diverse }

public static class KoblingsKategoriExtensions
{
    public static string Visningsnavn(this KoblingsKategori kategori) => kategori switch
    {
        KoblingsKategori.ARX => "ARX",
        KoblingsKategori.Salto => "Salto",
        KoblingsKategori.Diverse => "Diverse",
        _ => kategori.ToString()
    };
}
