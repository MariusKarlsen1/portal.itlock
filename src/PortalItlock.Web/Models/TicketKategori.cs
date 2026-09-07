namespace PortalItlock.Web.Models;

public class TicketKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public TicketKategoriGruppe Gruppe { get; set; }
    public int Rekkefolge { get; set; }
}
