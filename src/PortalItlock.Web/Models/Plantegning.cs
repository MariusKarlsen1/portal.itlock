using System.ComponentModel.DataAnnotations.Schema;

namespace PortalItlock.Web.Models;

public class Plantegning
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Navn { get; set; }
    public string? Byggetrinn { get; set; }

    public required string Filnavn { get; set; }
    public required string ContentType { get; set; }
    // Selve tegningsfilen ligger i samme tabell, men er skilt ut i PlantegningFil
    // slik at den ikke lastes hver gang plantegningen (eller en dør med
    // .Include(d => d.Plantegning)) hentes - det gjorde sidene trege. Der filen
    // trengs må spørringen ha .Include(p => p.Fil), ellers er Data tom.
    public PlantegningFil? Fil { get; set; }

    [NotMapped]
    public required byte[] Data
    {
        get => Fil?.Data ?? [];
        set
        {
            if (Fil is null)
            {
                Fil = new PlantegningFil { Id = Id, Data = value };
            }
            else
            {
                Fil.Data = value;
            }
        }
    }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    // Revisjon av tegningen. Øker hver gang en ny fil lastes opp med samme navn;
    // de gamle filene ligger i Revisjoner (PlantegningRevisjon).
    public int Revisjon { get; set; }
    public DateTime? RevidertDato { get; set; }
    public List<PlantegningRevisjon> Revisjoner { get; set; } = [];

    public List<Dor> Dorer { get; set; } = [];
}
