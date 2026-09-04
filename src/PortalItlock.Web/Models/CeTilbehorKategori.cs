namespace PortalItlock.Web.Models;

public enum CeTilbehorKategori
{
    Ingen,
    DorAutomatikk,
    SikkerhetsTilbehor,
    AnnetTilbehor
}

public static class CeTilbehorKategoriExtensions
{
    public static string Visningsnavn(this CeTilbehorKategori kategori) => kategori switch
    {
        CeTilbehorKategori.Ingen => "Ingen",
        CeTilbehorKategori.DorAutomatikk => "Dørautomatikk",
        CeTilbehorKategori.SikkerhetsTilbehor => "Sikkerhetstilbehør",
        CeTilbehorKategori.AnnetTilbehor => "Annet tilbehør",
        _ => kategori.ToString()
    };
}
