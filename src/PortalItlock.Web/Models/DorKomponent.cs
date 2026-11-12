namespace PortalItlock.Web.Models;

public class DorKomponent
{
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int Antall { get; set; } = 1;
    public string? Enhet { get; set; }
    public DorKomponentPlassering? Plassering { get; set; }
    public LevertAv LevertAv { get; set; } = LevertAv.F;

    public bool Montert { get; set; }

    // Bare beslag vi leverer (F) monteres og hakes av av oss. Ld/Le leveres
    // komplett av dørprodusent/elektro. Ikke lagret - regnes fra LevertAv.
    public bool MonteresAvOss => LevertAv == LevertAv.F;
    public DateTime? MontertDato { get; set; }

    public int? MontertAvBrukerId { get; set; }
    public Bruker? MontertAvBruker { get; set; }
}
