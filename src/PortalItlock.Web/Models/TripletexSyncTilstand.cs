namespace PortalItlock.Web.Models;

// Enkel enrads-tilstand (Id alltid 1) for å huske hvor langt kundesynkroniseringen
// mot Tripletex har kommet - unngår å måtte lese ALLE kunder på nytt hver gang
// bakgrunnsjobben kjører (se TripletexSyncService/TripletexSyncBackgroundService).
public class TripletexSyncTilstand
{
    public int Id { get; set; } = 1;
    public DateTime? SistSynkronisertKunderUtc { get; set; }
}
