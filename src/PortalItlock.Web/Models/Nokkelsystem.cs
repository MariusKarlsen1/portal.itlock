namespace PortalItlock.Web.Models;

public class Nokkelsystem
{
    public int Id { get; set; }
    public required string Systemnummer { get; set; }

    public string? Kundenavn { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Fabrikat { get; set; }
    public string? Type { get; set; }
    public string? Notater { get; set; }
    public NokkelsystemStatus Status { get; set; } = NokkelsystemStatus.Aktiv;

    public byte[]? BildeData { get; set; }
    public string? BildeContentType { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.UtcNow;

    public List<Rekvirent> Rekvirenter { get; set; } = [];
    public List<SystemVedlegg> Vedlegg { get; set; } = [];
    public List<NokkelKvittering> Kvitteringer { get; set; } = [];
    public List<NokkelsystemHendelse> Hendelser { get; set; } = [];
}
