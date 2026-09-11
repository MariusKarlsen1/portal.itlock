namespace PortalItlock.Web.Models;

public class FravarSoknad
{
    public int Id { get; set; }
    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }

    public FravarType Type { get; set; }
    public DateTime FraDato { get; set; }
    public DateTime TilDato { get; set; }
    public string? Kommentar { get; set; }

    public int? TilOppfolgingHosBrukerId { get; set; }
    public Bruker? TilOppfolgingHosBruker { get; set; }
    public bool HeleDagen { get; set; } = true;
    public TimeOnly? StartTid { get; set; }
    public TimeOnly? SluttTid { get; set; }

    public FravarStatus Status { get; set; } = FravarStatus.Venter;
    public DateTime OpprettetDato { get; set; } = DateTime.Now;

    public DateTime? BehandletDato { get; set; }
    public int? BehandletAvBrukerId { get; set; }
    public Bruker? BehandletAvBruker { get; set; }
    public string? AvslagsBegrunnelse { get; set; }
}
