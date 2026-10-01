namespace PortalItlock.Web.Models;

// Helt adskilt fra Bruker (som lever i hver kundes egen database): dette er
// kontoen(e) som har tilgang til /plattform - å opprette og administrere
// kunder. Lever i PlatformDbContext, ikke i noen kundes egen database, slik
// at ingen Admin-rolle hos en kunde (heller ikke itlock AS sin egen) noensinne
// automatisk gir tilgang hit.
public class PlattformBruker
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public required string Epost { get; set; }
    public string? PasswordHash { get; set; }
}
