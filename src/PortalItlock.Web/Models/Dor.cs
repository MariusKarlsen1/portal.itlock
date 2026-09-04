namespace PortalItlock.Web.Models;

public class Dor
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int? PlantegningId { get; set; }
    public Plantegning? Plantegning { get; set; }

    public required string Dornummer { get; set; }
    public string? Romnr { get; set; }
    public string? DorTil { get; set; }

    // Posisjon på plantegningen, i prosent (0-100) av bildets bredde/høyde.
    // Satt først når døren er plassert på en tegning.
    public double? PosX { get; set; }
    public double? PosY { get; set; }

    public string? Etasje { get; set; }
    public string? Sone { get; set; }
    public string? Dortype { get; set; }
    public string? BxH { get; set; }
    public string? Slagretning { get; set; }
    public string? Notater { get; set; }

    public string? Brann { get; set; }
    public string? Lyd { get; set; }
    public string? Energi { get; set; }
    public bool? FriBredde086 { get; set; }
    public int? Bredde { get; set; }
    public int? Hoyde { get; set; }

    public int? DorIdMalId { get; set; }
    public DorIdMal? DorIdMal { get; set; }

    public MontasjeStatus Status { get; set; } = MontasjeStatus.IkkeStartet;
    public DateTime? MontertDato { get; set; }

    public int? MontertAvBrukerId { get; set; }
    public Bruker? MontertAvBruker { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public DateTime? OppdatertDato { get; set; }
    public int? OppdatertAvBrukerId { get; set; }
    public Bruker? OppdatertAvBruker { get; set; }

    public List<DorKomponent> Komponenter { get; set; } = [];
    public List<DorFunksjon> Funksjoner { get; set; } = [];
}
