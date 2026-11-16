namespace PortalItlock.Web.Models;

// Branntegning knyttet til en plantegning. Vises inne på plantegningen som en
// egen visning ("Branntegning"), med dørene og utstyret på samme posisjon som
// på den vanlige tegningen (posisjonene er lagret i prosent av tegningen).
// Én branntegning per plantegning; laster man opp en ny, erstattes den gamle.
public class PlantegningBrann
{
    public int Id { get; set; }
    public int PlantegningId { get; set; }
    public Plantegning? Plantegning { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public DateTime OpplastetDato { get; set; } = DateTime.UtcNow;
}
