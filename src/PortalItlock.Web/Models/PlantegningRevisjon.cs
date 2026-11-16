namespace PortalItlock.Web.Models;

// En tidligere versjon av en plantegning. Når en ny tegning lastes opp med
// samme navn i samme prosjekt, flyttes den gamle filen hit og den nye tar
// plassen. Dører og utstyr blir stående på plantegningen med samme posisjon,
// så man kan bytte mellom revisjonene og se hva som er endret på tegningen.
public class PlantegningRevisjon
{
    public int Id { get; set; }
    public int PlantegningId { get; set; }
    public Plantegning? Plantegning { get; set; }

    // Revisjonsnummeret filen hadde mens den var gjeldende (0 = første opplasting).
    public int Revisjon { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    // Når filen ble lastet opp, og når den ble erstattet av en nyere.
    public DateTime OpplastetDato { get; set; }
    public DateTime ErstattetDato { get; set; } = DateTime.UtcNow;
}
