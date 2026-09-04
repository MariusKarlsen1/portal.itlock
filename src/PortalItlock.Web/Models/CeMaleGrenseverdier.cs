namespace PortalItlock.Web.Models;

// Én rad med gjeldende standard-grenseverdier (iht. NS-EN 16005) brukt til å
// validere målte verdier i CE-godkjenningens Measurements-steg.
public class CeMaleGrenseverdier
{
    public int Id { get; set; }

    public double MaksApningstidSek { get; set; } = 4;
    public double MaksLukketidHoySek { get; set; } = 3;
    public double MaksLukketidLavSek { get; set; } = 1.5;
    public double MaksApningskraftN { get; set; } = 67;
}
