namespace PortalItlock.Web.Models;

/// <summary>
/// Markerer at en nøkkel åpner en gitt sylinder (DorKomponent). Ett kryss i låsplan-matrisen.
/// </summary>
public class NokkelSylinder
{
    public int NokkelId { get; set; }
    public Nokkel? Nokkel { get; set; }

    public int DorId { get; set; }
    public int ComponentId { get; set; }
}
