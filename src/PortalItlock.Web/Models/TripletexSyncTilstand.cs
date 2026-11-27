namespace PortalItlock.Web.Models;

// Enkel enrads-tilstand (Id alltid 1) for å huske hvor langt kundesynkroniseringen
// mot Tripletex har kommet - unngår å måtte lese ALLE kunder på nytt hver gang
// bakgrunnsjobben kjører (se TripletexSyncService/TripletexSyncBackgroundService).
public class TripletexSyncTilstand
{
    public int Id { get; set; } = 1;
    public DateTime? SistSynkronisertKunderUtc { get; set; }

    // Tripletex-kontoen kundenumrene hører til («test:123» / «prod:123»). Byttes
    // kontoen, nullstilles koblingene og kundene sendes til den nye.
    public string? TripletexKonto { get; set; }

    // Checksum (versionDigest) for produktlisten. Uendret liste gir 304 fra
    // Tripletex, og varesynken hopper over resten.
    public string? ProduktDigest { get; set; }

    // Webhooks: Tripletex varsler /tripletex/webhook/{organisasjon} når kunder,
    // kontakter, produkter eller fakturaer endres, med denne hemmeligheten i
    // headeren. Da trengs bare en nattlig full synk som sikkerhetsnett.
    public string? WebhookHemmelighet { get; set; }
    public string? WebhookAdresse { get; set; }
    public bool WebhookAktiv { get; set; }
    public string? WebhookFeil { get; set; }
    public DateTime? SistFullSynkUtc { get; set; }
}
