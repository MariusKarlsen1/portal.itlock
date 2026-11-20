namespace PortalItlock.Web.Models;

// Sikkerhet for organisasjonen: hvilke roller som må bruke tofaktor-innlogging.
public class SikkerhetInnstillinger
{
    public int Id { get; set; }

    // Kommaliste med roller (BrukerRolle), f.eks. "Admin,Prosjektleder".
    public string? KrevTofaktorRoller { get; set; }
}
