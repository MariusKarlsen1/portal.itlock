namespace PortalItlock.Web.Models;

public enum KunngjoringKategori { Info, Firmatur, VunnetProsjekt, Nyansatt }

public static class KunngjoringKategoriExtensions
{
    public static string Visningsnavn(this KunngjoringKategori kategori) => kategori switch
    {
        KunngjoringKategori.Info => "Info",
        KunngjoringKategori.Firmatur => "Firmatur",
        KunngjoringKategori.VunnetProsjekt => "Vunnet prosjekt",
        KunngjoringKategori.Nyansatt => "Nyansatt",
        _ => kategori.ToString()
    };

    public static string PillFarge(this KunngjoringKategori kategori) => kategori switch
    {
        KunngjoringKategori.Info => "blaa",
        KunngjoringKategori.Firmatur => "lilla",
        KunngjoringKategori.VunnetProsjekt => "gronn",
        KunngjoringKategori.Nyansatt => "gul",
        _ => "noytral"
    };

    public static string PillIkon(this KunngjoringKategori kategori) => kategori switch
    {
        KunngjoringKategori.Info => "mail",
        KunngjoringKategori.Firmatur => "heart",
        KunngjoringKategori.VunnetProsjekt => "trend-up",
        KunngjoringKategori.Nyansatt => "users",
        _ => "mail"
    };
}
