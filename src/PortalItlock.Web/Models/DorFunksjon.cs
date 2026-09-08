namespace PortalItlock.Web.Models;

public class DorFunksjon
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Forkortelse { get; set; }

    public List<Dor> Dorer { get; set; } = [];
    public List<Package> Pakker { get; set; } = [];
}
