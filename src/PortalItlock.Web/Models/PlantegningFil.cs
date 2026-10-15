namespace PortalItlock.Web.Models;

// Tegningsfilen til en plantegning. Ligger i samme tabell (Plantegninger), men
// som egen entitet (table splitting) slik at den bare lastes med
// .Include(p => p.Fil) der den faktisk brukes - se Plantegning.Fil.
public class PlantegningFil
{
    public int Id { get; set; }
    public required byte[] Data { get; set; }
}
