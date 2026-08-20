using System.ComponentModel.DataAnnotations.Schema;

namespace PortalItlock.Web.Models;

public class Bruker
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public required string Epost { get; set; }
    public string? PasswordHash { get; set; }
    public string? Stilling { get; set; }
    public BrukerRolle Rolle { get; set; } = BrukerRolle.Montor;
    public bool Aktiv { get; set; } = true;

    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public List<Arbeidsordre> Arbeidsordre { get; set; } = [];
    public List<Timeregistrering> Timeregistreringer { get; set; } = [];
    public List<DorKomponent> MonterteKomponenter { get; set; } = [];
    public List<Prosjekt> Prosjekter { get; set; } = [];

    [NotMapped]
    public string Initialer => string.Concat(
        Navn.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(del => char.ToUpperInvariant(del[0])));
}
