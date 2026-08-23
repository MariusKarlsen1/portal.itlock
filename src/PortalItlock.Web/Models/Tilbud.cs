namespace PortalItlock.Web.Models;

public class Tilbud
{
    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public required string Tittel { get; set; }
    public TilbudPrisType PrisType { get; set; } = TilbudPrisType.Dekningsgrad;
    public decimal Prosentsats { get; set; }
    public decimal Timepris { get; set; }
    public decimal? EstimertTimerOverride { get; set; }
    public decimal? Montasjekost { get; set; }
    public decimal? MontasjeInnpris { get; set; }
    public string? Forside { get; set; }

    // PDF-visningsvalg
    public bool VisEnhetspris { get; set; } = true;
    public bool SummerAlleBeslag { get; set; } = true;
    public bool VisAlleDorerFraBeslagsliste { get; set; }
    public bool SkjulVarenummerISammendrag { get; set; }
    public bool VisPrisPerDor { get; set; }
    public string? ByggetrinnFilter { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;
    public DateTime? OppdatertDato { get; set; }

    public List<TilbudLinje> Linjer { get; set; } = [];
}
