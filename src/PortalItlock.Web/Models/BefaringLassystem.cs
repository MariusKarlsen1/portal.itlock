namespace PortalItlock.Web.Models;

public class BefaringLassystem
{
    public int Id { get; set; }
    public int BefaringDorfeltId { get; set; }
    public BefaringDorfelt? Dorfelt { get; set; }

    // "Daglås" eller "Nattlås"
    public required string Type { get; set; }

    public string? Laskasse { get; set; }
    public string? MekSluttstykke { get; set; }
    public string? Mikrobryter { get; set; }
    public string? ElSluttstykke { get; set; }
    public string? Stolpe { get; set; }
    public string? Volt { get; set; }
    public string? Karmoverforing { get; set; }
    public string? Festelepper { get; set; }
    public string? Kabel { get; set; }
    public string? Dorvrider { get; set; }
    public string? Skilt { get; set; }
    public string? Overflate { get; set; }
    public string? Sylinder { get; set; }
    public string? DortykkelseAB { get; set; }
    public string? Magnetkontakt { get; set; }
    public string? Nodutstyr { get; set; }
    public string? AnnetUtstyr { get; set; }
}
