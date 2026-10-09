namespace PortalItlock.Web.Models;

public class KoblingsKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public string Ikon { get; set; } = "krets-brikke";
    public string Farge { get; set; } = "oransje";
    public int Rekkefolge { get; set; }

    public List<KoblingsSkjema> Skjemaer { get; set; } = [];
}
