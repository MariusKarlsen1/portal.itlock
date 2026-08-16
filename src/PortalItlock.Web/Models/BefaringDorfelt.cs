using System.ComponentModel.DataAnnotations.Schema;

namespace PortalItlock.Web.Models;

public class BefaringDorfelt
{
    public int Id { get; set; }
    public int BefaringId { get; set; }
    public Befaring? Befaring { get; set; }

    // Identifikasjon
    public string? Dornr { get; set; }
    public string? Dornavn { get; set; }
    public string? Dortype { get; set; }
    public string? Floyer { get; set; }
    public string? BxH { get; set; }
    public string? Slagretning { get; set; }
    public string? Lassystemnr { get; set; }

    // Krav
    public string? Brannkrav { get; set; }
    public string? Brannklasse { get; set; }
    public string? Fg { get; set; }
    public string? Sikringsklasse { get; set; }
    public string? Risikoklasse { get; set; }
    public string? UniversellUtforming { get; set; }
    public string? ApnekraftMaks30N { get; set; }

    // Dørlukker
    public string? Dorlukker { get; set; }
    public string? ArmGlideskinne { get; set; }
    public string? VkPlate { get; set; }
    public string? MontasjeSideDorlukker { get; set; }
    public string? AnnetUtstyrDorlukker { get; set; }

    // Automatikk
    public string? Automatikk { get; set; }
    public string? TrekkSkyvArm { get; set; }
    public string? Adapter { get; set; }
    public string? MontasjeSideAutomatikk { get; set; }
    public string? Albuekontakter { get; set; }
    public string? RadarSensor { get; set; }
    public string? KabelAutomatikk { get; set; }
    public string? UpsNodstrom { get; set; }
    public string? Sikkerhetssensor { get; set; }

    // Øvrig beslag
    public string? Magnetlas { get; set; }
    public string? BrakettMl { get; set; }
    public string? Panikkbeslag { get; set; }
    public string? Handtak { get; set; }
    public string? AnnetUtstyrOvrig { get; set; }

    public string? Notater { get; set; }

    public List<BefaringLassystem> Lassystemer { get; set; } = [];
    public List<BefaringDorfeltBilde> Bilder { get; set; } = [];

    [NotMapped]
    public BefaringLassystem Daglas => GetOrAddLassystem("Daglås");

    [NotMapped]
    public BefaringLassystem Nattlas => GetOrAddLassystem("Nattlås");

    private BefaringLassystem GetOrAddLassystem(string type)
    {
        var existing = Lassystemer.FirstOrDefault(l => l.Type == type);
        if (existing is not null)
        {
            return existing;
        }

        var created = new BefaringLassystem { Type = type };
        Lassystemer.Add(created);
        return created;
    }
}
